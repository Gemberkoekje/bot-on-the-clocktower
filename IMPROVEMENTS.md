# Improvements, Missing Features, and Concerns

This document is a living, opinionated list of things that could make Bot on the Clocktower better at hosting Blood on the Clocktower games on Discord. It deliberately ranges from concrete code-level issues to higher-level product gaps. Entries are not prioritized; treat this as an idea pool, not a roadmap.

For an overview of what the bot *does* implement today see [ARCHITECTURE.md](ARCHITECTURE.md).

---

## 1. Gameplay Features Not Currently Supported

These are Blood on the Clocktower mechanics or storytelling conveniences that the bot does not yet handle, or handles only partially.

### 1.1 Travelers
- The bot has no first-class notion of a **Traveler** joining or leaving mid-game. The current advice (run `/currGame` from the Python era; in C# this is implicit in `/night`) is workable but undocumented.
- A `/traveler add @user` / `/traveler remove @user` command pair could grant/revoke the Villager role and the **(ST)** nickname behavior cleanly, and update the in-flight `GameMetricRecord`.

### 1.2 Fabled, Bootlegger, and out-of-script characters
- `/lookup` queries the official character list and any registered custom scripts, but there is no concept of a **Fabled** character that may appear in any script. They have to be added to a custom script manually.

### 1.3 Mid-game character info distribution
- `/evil` and `/lunatic` cover the standard demon-team and Lunatic info.
- There is **no** built-in helper for sending:
  - **Demon bluffs** (3 not-in-play good characters) to the Demon — currently the Storyteller does this manually.
  - **Washerwoman / Librarian / Investigator / Chef / Empath / Fortune Teller / Steward / Knight / Noble / Shugenja / High Priestess** etc. first-night/other-night info.
  - **Spy / Widow** grimoire-viewing flows (today this is achieved by screen sharing into a Cottage).
- A generic `/whisper @player <text>` command, scoped to the Storyteller, would cover most ad-hoc cases without modeling every character.

### 1.4 Nominations and voting
- `/vote` only **moves players to Town Square**. It does not run a nomination or tally a vote.
- A `/nominate <nominator> <nominee>` command could:
  - Post a public nomination message.
  - Open a 10–20 s "hand up / hand down" component dialog (or open a short component-based vote).
  - Track dead-vote tokens per player.
  - Produce a final tally and a transcript.
- Even without full automation, recording the **nomination order** would help long games.

### 1.5 Day-phase timers and reminders
- `/voteTimer` exists for the open-nominations countdown. There is no timer for **first day discussion length**, **private conversation length**, or **end-of-game grace period**.
- A generalized `/timer <name> <duration>` with pinned reminder messages would be cheap to add on top of the existing `ICallbackScheduler<TKey>`.

### 1.6 Persistent Storyteller notes / grimoire
- The bot doesn't store any per-game state beyond participation/metric counts. A "grimoire" feature — even just a Storyteller-only ephemeral text channel that survives across days — would be valuable. Long-term, a structured grimoire (character → player → reminder tokens) would be game-changing.

### 1.7 Scripts and rolesets
- `/addScript` accepts a Bloodstar-style script JSON URL but does not:
  - Validate the script's role distribution (Townsfolk/Outsider/Minion/Demon balance per player count).
  - Generate a recommended roleset for a given player count.
  - Pin the active script to a town so `/lookup` defaults to in-script characters.

### 1.8 Reminder tokens and night order
- The bot does not display the **night-order list** for the active script in the Storyteller control channel. Showing first-night and other-nights order is one of the highest-impact UX additions possible.

### 1.9 Replays / transcripts
- There is no command to export a game summary (players, Storyteller, length, days, nights, votes, outcome) at end-game. The `GameMetricRecord` already captures most of the raw data — surfacing it would just need a `/summary` command.

### 1.10 Multiple simultaneous Storytellers across towns
- A user with `/storytellers` can co-Storytell within a town. There is no cross-town co-Storytelling — useful for events where one person spins up several parallel games on a single Discord server.

---

## 2. Discord UX Improvements

### 2.1 Better controls
- The `/game` panel uses buttons for Day/Night/Vote/End and a select for vote-timer presets. It could also include:
  - A **"Pause"** button that disables auto-cleanup for this town for N hours.
  - A **"Move everyone to Town Square"** button independent of vote semantics.
  - **"Open private rooms"** / **"Lock private rooms"** for daytime semi-private chats.

### 2.2 Ephemeral feedback
- Many gameplay commands respond publicly in the control channel. For Storyteller-only operations, **ephemeral responses** would reduce control-channel noise.

### 2.3 Localization
- All user-visible strings are hard-coded English. There's no `IStringLocalizer` or resource file. Migrating to `Microsoft.Extensions.Localization` would unlock community translations.

### 2.4 Slash command discoverability
- Several commands have similar prefixes (`/createTown`, `/destroyTown`, `/addTown`, `/removeTown`, `/modifyTown`, `/townInfo`). Grouping under `/town create|destroy|add|remove|modify|info` (a Discord subcommand group) would be cleaner and reduce the global command-name cap consumed by this bot.

### 2.5 Permissions
- The bot relies on **role hierarchy** to manage `(ST)` nicknames and to grant `Storyteller`/`Villager` roles. A pre-flight `/diagnose` command that verifies its own role is high enough and that all required permissions are present would save a lot of "why doesn't this work?" support requests.

---

## 3. Code-Level Concerns

These are observations about the current C# codebase that would benefit from cleanup or hardening.

### 3.1 `GameMetricDatabase.UpsertByTownHash`
- The method does a **delete-then-store** in the same `LightweightSession`. Because `TownHash` is the document identity and the schema allows multiple `Complete`-flagged records to share a hash, the implementation is correct but subtle. Two observations:
  1. There is **no transaction isolation** between the `FirstOrDefaultAsync` lookup and the `Store`. Two concurrent writes for the same town could both observe "no existing record" and create duplicates. A unique partial index on `(TownHash) WHERE Complete = false` would make the invariant explicit.
  2. The `using var session` is opened **twice** in `RecordGameAsync` (once to mark the existing in-flight record `Complete = true`, once to store the new in-flight record). These can be merged into a single session.

### 3.2 `RecordGameAsync` writes outside of `await`
- In `RecordGameAsync` the first `session.Store(existing); await session.SaveChangesAsync();` is inside a `using var` whose scope leaks past the `await` because there is no inner block — actually fine for `using var`, but the style is inconsistent with the second write that *does* use a `using (…)` block. Pick one.

### 3.3 Magic timeouts
- Cleanup time (5 hours), ghost-town threshold (30 days), and the cleanup check cadence (1 hour) are hard-coded `TimeSpan` literals in `TownCleanup`/`GhostTownCleanup`. These should be configurable via `appsettings.json`.

### 3.4 Allow-list in `Announcer`
- The hard-coded guild ID `128585855097896963` in `Announcer.s_guildAllowList` is a "restricted announcement" allow-list. This belongs in configuration (`Announcements:RestrictedAllowList`), not in source.

### 3.5 Logging hygiene
- The Serilog destructuring rules live inside `Program.Main`. They should move into the projects that own the destructured types (e.g. a `LoggingExtensions` in `Bot.Remora`) so adding a new wrapper type doesn't silently log gigabytes of role lists.
- The bot logs `Bot started at … using C# runtime …` via both `Console.WriteLine` and `Log.Information`. Once Serilog's console sink is enabled in dev, this is duplicated.

### 3.6 Connection string fallbacks
- `Bot.Database.DependencyInjection.ResolveConnectionString` reads `ConnectionStrings:Postgres`, `ConnectionStrings__Postgres`, **and** `POSTGRES_CONNECT` from `IEnvironment`. The first two are conventionally read from `IConfiguration`, not from environment variables. Consolidating on `IConfiguration` and treating `POSTGRES_CONNECT` as the only legacy fallback would be clearer.

### 3.7 Component leak
- `IComponentService` registers callbacks keyed by component ID for the lifetime of the process. Long-running bots will accumulate dead callbacks for messages whose Storyteller never clicked any button. A TTL or weak reference would help, as would unregistering when the parent town ends a game.

### 3.8 Test coverage gaps
- `RemoraClient.DispatchComponentInteractionAsync` is tested for happy/no-handler paths but not for exceptions thrown inside a component callback.
- `GhostTownCleanup` has limited tests for the 30-day threshold boundary.
- `BotGameplay.CreateGameFromDiscordState` is hard to test exhaustively because of the implicit Storyteller-swap logic; consider extracting `ResolveStorytellers(allUsers, commandAuthor)` into a pure function.

### 3.9 Documentation drift
- `plans/mongodb-to-postgres-marten.md` still describes `ServiceFactory.RegisterServices(sp)` — that registration point was renamed to `DependencyInjection.AddBotDatabaseServices`. Either rewrite the plans as historical references or delete them now that the migrations are complete.

### 3.10 .NET version messaging
- `Program.Main` logs `Environment.Version`, which is the **CLR** version, not the **.NET** version. On .NET 10 it prints something like `10.0.0` already, but on prerelease SDKs the value can be confusing. Logging `RuntimeInformation.FrameworkDescription` is more useful.

---

## 4. Operational Concerns

### 4.1 Backups
- No documented backup strategy for the PostgreSQL database. The data is small (town registrations, lookup scripts, metrics) but losing it forces every server to re-run `/createTown` and `/addScript`. A simple nightly `pg_dump` cron and a brief restore runbook should ship with the deployment docs.

### 4.2 Migrations
- Marten manages its own schema, but no command exists to **export** or **import** the bot's data, or to clone production data into a staging environment for debugging.

### 4.3 Observability
- The bot logs to a file and (in dev) to console. There are no metrics (`Meter`/OpenTelemetry) and no health endpoint. Adding:
  - An OpenTelemetry exporter (Serilog → OTLP is straightforward).
  - A `/internal-health` HTTP endpoint or a heartbeat record in Postgres.
- This would make multi-instance hosting feasible.

### 4.4 Rate-limit handling
- The README acknowledges Discord rate limits during `/night` and `/day`. The current strategy (shuffle, then move serially) is fine, but:
  - There is no visible **progress indicator** during long moves. Editing the deferred response with "Moved 12/18 players…" would reduce support questions.
  - Failed moves are logged but not retried.

### 4.5 Multi-instance / horizontal scaling
- A single process holds the gateway connection. Discord supports sharding for very large bots but the code does not. While unlikely to matter for this product, it would prevent simple blue-green deployments.

### 4.6 Secret handling
- The Discord token ends up in memory and may leak into logs if `BotConfiguration` is ever logged directly. A `[SensitiveData]` attribute plus a Serilog destructuring rule for `DiscordConfiguration` would prevent regressions.

### 4.7 Self-hosting docs
- There is no Docker image, no docker-compose example combining the bot with PostgreSQL, and no Kubernetes manifest. A reference `docker-compose.yml` and a `Dockerfile` in the repo would dramatically lower the bar for new self-hosters.

---

## 5. Security and Abuse

### 5.1 Storyteller authority
- Any user who runs `/game`, `/night`, etc. **becomes** the Storyteller (replacing the previous one). On a friendly server this is fine; on a public-bot install it can be abused to grief in-progress games. Options:
  - Require the user to already hold the server-wide Storyteller role configured at `/createTown`, if one is set.
  - Require co-Storyteller confirmation via a button.

### 5.2 Custom script URLs
- `/addScript` accepts any URL and the bot fetches it. There is no SSRF protection (e.g. the bot would happily try to fetch `http://localhost:…/`) and no size cap on the response. The fetcher (`StringDownloader`) should reject private/loopback IPs, set a strict timeout, and cap the response body size.

### 5.3 Audit log
- There is no append-only audit log of `/createTown`, `/destroyTown`, `/addScript`, `/removeScript`, `/addTown`, `/removeTown`, `/announce` actions. Adding one (either to Postgres or to a per-guild text channel) would help servers recover from accidents.

---

## 6. Process and Tooling

- **Pre-commit hooks** — there is a `copilot-instructions.md` rule about trimming trailing whitespace; enforcing it via `dotnet format --verify-no-changes` in CI would catch drift.
- **`Directory.Packages.props` audit** — package versions are centrally managed (good), but the bot has no automated dependency-update flow (e.g. Dependabot or Renovate) checked into the repo.
- **Issue templates and contributing guide** — there is no `CONTRIBUTING.md` or GitHub issue template. Adding one (with the "include logs", "include `/towninfo` output" prompts) reduces support churn.

---

## 7. Documentation Gaps (Player-Facing)

The current docs are good for setup and command reference but underserve a few audiences:

- **Players**, as opposed to Storytellers — there is no one-pager explaining what each button does from the *player* perspective.
- **First-time Storytellers** — a short "your first game" walkthrough covering `/createTown` → `/storytellers` → `/game` → `/night` → `/day` → `/vote` → `/endGame`.
- **Troubleshooting** — common failure modes (bot role too low, missing permissions, players outside Town Square when `/night` runs, etc.) deserve a dedicated section in `SETUP.md` or a new `TROUBLESHOOTING.md`.

---

## 8. Out-of-Scope but Worth Considering

These are intentionally broader product ideas that would require significant new work.

- **Web dashboard** for server admins to view metrics, edit lookup scripts, and review audit logs without DMing the bot owner.
- **Integration with [clocktower.online](https://clocktower.online/)** so the bot can read the active script and mirror player names automatically.
- **Voice activity hints** — show the Storyteller a "who's currently speaking in the Cottage" indicator (Discord doesn't expose this directly, but voice-state-update events can approximate it).
- **AI-assisted Storyteller helpers** — character-info text generation, neighbor calculation, possible-roles narrowing. Strictly opt-in.
- **Bring-your-own-character art** — `/lookup` could use script-supplied icons when present (Bloodstar already provides these).

---

## Contributing

If you want to tackle any of the above, please open a GitHub issue first with the specific item, your proposed design, and any breaking-change considerations. Small, atomically-committed slices (in the spirit of the existing DI refactor guidance in `.github/copilot-instructions.md`) are easiest to review.
