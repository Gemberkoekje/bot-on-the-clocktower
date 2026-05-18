# Bot on the Clocktower — Architecture and Implementation

This document describes the current C# implementation of Bot on the Clocktower. It is intended for contributors, reviewers, and operators of self-hosted instances. For player-facing documentation see [README.md](README.md), [COMMANDS.md](COMMANDS.md), and [SETUP.md](SETUP.md). For configuration see [CONFIGURATION.md](CONFIGURATION.md).

---

## 1. High-Level Overview

Bot on the Clocktower is a Discord bot that automates the channel, role, and voice-movement bookkeeping required to run a game of **Blood on the Clocktower** on Discord. Players gather in a "Town" (a category of voice channels), and the bot moves them between the Town Square, private daytime channels, and individual nighttime Cottages in response to slash commands run by the Storyteller.

The bot is a long-running .NET 10 console application. It connects to Discord over a single gateway connection, persists state to PostgreSQL via [Marten](https://martendb.io/), and shuts down cleanly on `Ctrl+C` / SIGTERM.

```
+--------------------+      gateway / REST      +-------------+
|  Discord users     | <----------------------> | Discord API |
+--------------------+                          +------+------+
													   |
													   v
+---------------------------------------------------------------+
|                       Bot.Main (host)                          |
|  - Builds IConfiguration                                       |
|  - Configures Serilog                                          |
|  - Registers services and runs BotSystemRunner                 |
+---------------------------------------------------------------+
			  |              |                |
			  v              v                v
	   +-----------+   +-----------+   +---------------+
	   | Bot.Core  |   | Bot.Remora|   | Bot.Database  |
	   | gameplay  |   | Discord   |   | Marten/Postgres|
	   +-----------+   +-----------+   +---------------+
							  \         /
							   v       v
							+-------------+
							|   Bot.Api   |
							| interfaces  |
							+-------------+
```

---

## 2. Solution Layout

The Visual Studio solution (`bot-on-the-clocktower.slnx`) contains the following projects:

| Project | Purpose |
| ------- | ------- |
| `Bot.Api` | Public interfaces and small DTOs. The dependency boundary for the rest of the codebase. Defines the abstractions for Discord (`IBotSystem`, `IBotClient`, `IGuild`, `IChannel`, `IMember`, `IRole`, builders…) and for persistence (`ITownDatabase`, `IGameActivityDatabase`, `IGameMetricDatabase`, `ICommandMetricDatabase`, `ILookupRoleDatabase`, `IAnnouncementDatabase`). |
| `Bot.Base` | Cross-cutting helpers shared by host and integration layers. |
| `Bot.Core` | Gameplay, setup, vote-timer, town-cleanup, lookup, and component-handling logic. Has no direct dependency on a Discord library — only on `Bot.Api`. |
| `Bot.Database` | Marten/PostgreSQL implementations of the `Bot.Api.Database` interfaces. |
| `Bot.Remora` | [Remora.Discord](https://github.com/Remora/Remora.Discord) implementations of `Bot.Api.Discord` and slash-command sources. |
| `Bot.Main` | Process entry point. Builds configuration, wires DI, and runs `BotSystemRunner`. |
| `Test.Bot.Base`, `Test.Bot.Core`, `Test.Bot.Database`, `Test.Bot.Remora`, `Test.Main` | xUnit/Moq test projects covering the corresponding production projects. |

The deprecated `Bot.DSharp`, `Test.Bot.DSharp`, the old MongoDB code, and the Python implementation (`README_PYTHON.md`) are no longer part of the runtime build path.

---

## 3. Process Lifecycle (`Bot.Main`)

`Bot.Main/Program.cs` is the entry point.

1. **Build configuration.** A `ConfigurationBuilder` is created with the following providers (later sources override earlier ones):
   - `appsettings.json`
   - `appsettings.{Environment}.json` (where `Environment` comes from `DOTNET_ENVIRONMENT` or `ASPNETCORE_ENVIRONMENT`, defaulting to `Production`)
   - `usersettings.json` (git-ignored)
   - User secrets (`AddUserSecrets<Program>`)
   - Environment variables
2. **Bind `BotConfiguration`.** The strongly-typed `BotConfiguration` object (Discord, Deployment, Logging) is bound from configuration.
3. **Configure Serilog.** A single static `Log.Logger` is built. Several Remora and domain types have custom destructuring rules to prevent log explosions (e.g. ignoring `RemoraMember.Roles`, transforming `RemoraGuild` to `{Id, Name}`).
4. **Load legacy `.env`** at `..\..\..\..\.env` for backward compatibility.
5. **Apply log level.** In `dev` deployments minimum level is `Debug` and the console sink is added when enabled; in `prod` it is `Information`. The `Logging:LogLevel:Default` setting can override this.
6. **Register services and run.** `RunAsync(IConfiguration)` creates a `ServiceCollection`, calls each layer's `Add…Services(…)` extension, builds the `IServiceProvider`, resolves `BotSystemRunner`, and awaits `RunAsync()`.
7. **Graceful shutdown.** `ProcessExit` and `Console.CancelKeyPress` cancel a shared `CancellationTokenSource` that downstream services observe via `IFinalShutdownService` / `IShutdownPreventionService`.

The runner itself is intentionally small:

```csharp
public class BotSystemRunner
{
	public async Task RunAsync()
	{
		await m_client.ConnectAsync();
		await m_finalShutdown.ReadyToShutdown;
		await m_client.DisconnectAsync();
	}
}
```

---

## 4. Dependency Injection

DI is built with `Microsoft.Extensions.DependencyInjection`. Each layer exposes an `Add…Services` extension on `IServiceCollection`:

- `services.AddBotBaseServices(configuration)` — `IEnvironment`, `IDateTime`, `ITask`, `IConfiguration` itself.
- `services.AddBotDatabaseServices()` — Marten factories, `IDocumentStore`, all `I…Database` singletons.
- `services.AddBotCoreServices(ct)` — process-logger factory, callback scheduler factory, interaction error handlers, shutdown service, shuffle service, lookup services.
- `services.AddBotGameplayServices()` — `BotGameplay`, `BotSetup`, `BotVoteTimer`, `BotMessaging`, `Announcer`, `LegacyCommandReminder`, `GhostTownCleanup`, `TownCleanup`, `TownMaintenance`, `BotSystemRunner`, and the interaction queue/wrapper pair.
- `services.AddRemoraServices()` — `IBotSystem`, `IBotClient`, builders, command sources, slash-command registry/registrar.

Singletons are used throughout — there is no per-request scope because a Discord bot has a single long-lived process.

---

## 5. Discord Integration (`Bot.Remora`)

`Bot.Remora` adapts Remora.Discord types to the `Bot.Api.Discord` interfaces so that the rest of the code never touches a Remora type directly.

### Key types

- **`RemoraSystem : IBotSystem`** — factory for embed/message/webhook builders, color builders, and component factories (button/select/text-input).
- **`RemoraClient : IBotClient`** — owns the gateway connection, raises `Connected` and `MessageCreated`, exposes guild lookups, and dispatches component interactions through `IComponentService`.
- **Wrapper types** — `RemoraGuild`, `RemoraChannel`, `RemoraChannelCategory`, `RemoraMember`, `RemoraRole`, `RemoraMessage`, `RemoraEmbed`, `RemoraColor`, `RemoraInteractionContext`, etc. They are thin adapters over Remora API objects and expose only what `Bot.Api` requires.
- **Slash command sources** — `RemoraGameSlashCommands`, `RemoraSetupSlashCommands`, `RemoraMessagingSlashCommands`, `RemoraLookupSlashCommands`, `RemoraMiscSlashCommands`. Each implements `IRemoraSlashCommandSource` and yields strongly-typed `IRemoraSlashCommand` instances. The full command list is documented in [COMMANDS.md](COMMANDS.md).
- **Command registration** — `RemoraCommandRegistrationPlanner` decides, based on `Deployment:Type` and `Discord:DevGuildIds`, whether to:
  - **dev**: register the command set to each dev guild (instant for testing), and skip global registration; or
  - **prod**: clear dev-guild commands and register globally.
  `IRemoraCommandRegistrar` is the abstraction that actually applies the plan against the Discord REST API.

### Interaction flow

1. A user runs `/createTown Ravenswood Bluff`.
2. Remora delivers the interaction to the command source's `InvokeAsync`, which forwards to the matching method on `IBotSetup`/`IBotGameplayInteractionHandler`/etc.
3. The handler wraps the operation in `IGuildInteractionWrapper.WrapInteractionAsync` (or `ITownInteractionWrapper`), which:
   - Defers the Discord response so the bot has the full 15-minute interaction window.
   - Queues the work on the per-guild / per-town `IInteractionQueue` to serialize concurrent commands on the same scope.
   - Runs the inner callback with an `IProcessLogger` that accumulates user-visible status messages.
   - Edits the deferred response with the resulting `InteractionResult` (message text + optional embeds).
4. Errors are caught by `IGuildInteractionErrorHandler` / `ITownInteractionErrorHandler` and surfaced to the user without crashing the bot.

### Components

Component (button/select) interactions go through `IComponentService`. Handlers register a callback for a component ID at the point they build the message; the next interaction with that ID is dispatched back to the callback inside a new interaction context.

---

## 6. Gameplay (`Bot.Core`)

### Town model

A *town* is the set of Discord objects the bot manages on behalf of one game:

- A **day category** with a **control text channel**, the **Town Square** voice channel, and an optional **chat text channel** and additional voice channels (Dark Alley, Library, …).
- An optional **night category** containing Cottage voice channels.
- A **Storyteller role** and a **Villager role** granted/revoked while a game is active.

The data record is `TownRecord` (`Bot.Database`), keyed by `(GuildId, ControlChannelId)` via the `TownKey` value object.

### Setup commands (`BotSetup`)

- `/createTown` — creates all categories, channels, and roles, then stores the resulting `TownRecord` and ACLs them against the optional server-wide Storyteller and Player roles.
- `/addTown` — registers an existing set of channels and roles as a town.
- `/modifyTown` — updates the chat channel and/or night category for an existing town.
- `/removeTown` / `/destroyTown` — un-register or fully delete the town.
- `/townInfo` — dumps the registered town details to the control channel.

### Gameplay commands (`BotGameplay` + `BotGameplayInteractionHandler`)

- `/game` — sends the Storyteller a control message with Day/Night/Vote/End Game buttons and a vote-timer dropdown.
- `/night` — `CreateGameFromDiscordState` enumerates the voice channels under the day category, computes the active player set, assigns the command author as the Storyteller (replacing any previous one), then shuffles villagers into Cottages.
- `/day` — moves everyone in the night category back to Town Square.
- `/vote` — pulls all players from any daytime voice channel into Town Square.
- `/voteTimer <time>` / `/stopVoteTimer` — `BotVoteTimer.VoteTimerController` keeps a per-town countdown that periodically posts time-remaining messages and finally invokes `IVoteHandler.PerformVoteAsync`.
- `/endGame` — clears roles and Storyteller nickname tags.
- `/storytellers` — explicitly designates one or more current players as Storytellers.

`BotGameplay` is itself an `IVoteHandler` and listens to `ITownCleanup.CleanupRequested` to auto-end stale games.

### Town cleanup

- `TownCleanup` records the last activity timestamp per town in `IGameActivityDatabase`. After a few hours of inactivity (`TimeSpan.FromHours(5)` by default, with a `TEST_RAPID_CLEANUP` debug shortcut) it raises `CleanupRequested`, which `BotGameplay` handles by calling `EndGameUnsafeAsync`.
- `GhostTownCleanup` runs as a maintenance task: for towns whose most recent game is older than 30 days *and* whose guild/control-channel can no longer be found on Discord, it deletes the `TownRecord` (the channels themselves are not touched).
- `TownMaintenance` is the maintenance-task host. Components subscribe by calling `AddMaintenanceTask`, and the host invokes them on startup and on relevant Discord events.

### Messaging (`BotMessaging`)

- `/evil` DMs the demon and minions about each other (and, optionally, the Magician).
- `/lunatic` DMs the Lunatic the same kind of message a real demon would receive.

### Lookup (`Bot.Core.Lookup`)

- `/lookup <name>` resolves a character by name, using both the official character list (cached by `OfficialCharacterCache`) and any per-guild custom scripts registered through `/addScript`.
- `CustomScriptCache` and `CustomScriptParser` parse Bloodstar-style script JSON, optionally surfacing `_meta.almanac` URLs and per-character `flavor` text.
- `LookupEmbedBuilder` formats results as Discord embeds, color-coded by team via `CharacterColorHelper`.
- `/refreshScripts` forces a re-fetch; otherwise scripts refresh daily.

### Announcements

`Announcer` posts a one-time message to each town's control channel when the bot is upgraded to a new significant version (versions are listed in `VersionProvider`). `IAnnouncementDatabase` records which guild has seen which version. Servers can opt out via `/announce false`. The `RESTRICT_ANNOUNCE` environment variable restricts announcements to a hard-coded internal allow-list during testing.

`LegacyCommandReminder` watches for legacy `!` prefix commands and gently nudges users toward the new slash commands.

---

## 7. Persistence (`Bot.Database`)

All persistence is implemented over **Marten** against a single PostgreSQL database. `MartenDocumentStoreFactory` configures the `IDocumentStore` with indexes for the documents listed below:

| Document | Identity | Indexed columns |
| -------- | -------- | --------------- |
| `TownRecord` | `Id` | `GuildId`, `ControlChannelId`, `DayCategory` |
| `GameActivityRecord` | (default) | `GuildId`, `ChannelId` |
| `LookupRoleRecord` | `Id` | `GuildId` |
| `AnnouncementRecord` | `Id` | `GuildId` |
| `GameMetricRecord` | `TownHash` | `Complete`, `FirstActivity` |
| `CommandMetricRecord` | `Id` | `Day` |

`GameMetricDatabase` (the currently open file) is representative of the pattern:

- Each interface method opens a short-lived Marten session (`QuerySession` for reads, `LightweightSession` for writes).
- An "in-flight" `GameMetricRecord` is identified by `TownHash == HashCode.Combine(GuildId, ControlChannelId)` and `Complete == false`.
- `RecordDayAsync` / `RecordNightAsync` / `RecordVoteAsync` increment counters on the open record; `RecordEndGameAsync` marks it `Complete = true`; `RecordGameAsync` closes any open record and creates a new one.
- `UpsertByTownHash` performs a delete-then-store inside a single session because `TownHash` is the document identity and multiple complete records can share it.

`DependencyInjection.AddBotDatabaseServices` resolves the connection string from (in order) `ConnectionStrings:Postgres`, `ConnectionStrings__Postgres`, then the legacy `POSTGRES_CONNECT` environment variable. Missing/blank values throw `InvalidPostgresConnectStringException` at startup.

---

## 8. Cross-Cutting Concerns

### Interaction queues

`GuildInteractionQueue` and `TownInteractionQueue` serialize work per guild / per town to avoid races such as two Storytellers pressing the Night button simultaneously. The associated `*Wrapper` types own the lifecycle (defer → enqueue → run → edit response → handle exceptions).

### Callback scheduler

`ICallbackSchedulerFactory` produces `ICallbackScheduler<TKey>` instances backed by a single timer per scheduler. It is the foundation for the vote timer and town-cleanup scheduling.

### Process logger

`IProcessLogger` accumulates human-readable status lines for an interaction and forwards them to Serilog. It is the bridge between deep gameplay logic (which doesn't know about Discord messages) and the user-visible interaction response.

### Shuffle service

`IShuffleService` (`ShuffleService`) is a seam over `Random` so tests can produce deterministic player orderings.

### Static seams

`Bot.Main` provides `DateTimeStatic` and `TaskStatic` implementations of `IDateTime` and `ITask` so tests can control time and `Task.Delay`.

---

## 9. Testing

- **`Test.Bot.Core`** covers the gameplay state machine, setup permission matrices, town cleanup scheduling, vote timer behavior, and lookup formatting using Moq-driven fakes of the `Bot.Api` interfaces.
- **`Test.Bot.Database`** exercises each `…Database` implementation against an in-memory or test-configured Marten instance.
- **`Test.Bot.Remora`** validates the Remora adapter layer: type-enforcement of builder inputs, connect/disconnect/event semantics, deploy-type validation, guild registration, component dispatch, and command registration plans.
- **`Test.Main`** verifies the configuration precedence wiring (`ProgramEnvironment`).
- **`Test.Bot.Base`** covers shared base primitives.

CI runs all of the above with `dotnet test` over the solution.

---

## 10. Build and Run

From the solution root:

```powershell
dotnet build
cd Bot.Main
dotnet run
```

In Visual Studio, set `Bot.Main` as the startup project and hit F5. Visual Studio sets `DOTNET_ENVIRONMENT=Development`, which causes the dev-mode code path to run.

---

## 11. Related Documents

- [README.md](README.md) — user-facing overview and setup.
- [COMMANDS.md](COMMANDS.md) — exhaustive slash-command reference.
- [SETUP.md](SETUP.md) — town/channel layout.
- [CONFIGURATION.md](CONFIGURATION.md) — full configuration reference.
- [CHANGELOG.md](CHANGELOG.md) — release notes.
- [IMPROVEMENTS.md](IMPROVEMENTS.md) — known gaps and ideas for future work.
- `plans/*.md` — historical migration plans (DSharpPlus → Remora, Mongo → Marten, .env → appsettings).
