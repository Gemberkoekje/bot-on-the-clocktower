# Change from DSharp to Remora Discord

## Goal
Replace the DSharpPlus-based Discord integration with Remora Discord while preserving the existing `Bot.Api` abstraction layer used by `Bot.Core`.

## Current State
- `Bot.Main\Program.cs` references `Bot.DSharp`, registers DSharp services, and starts `new BotSystemRunner(sp, new DSharpSystem())`.
- `Bot.DSharp` contains adapter/wrapper types implementing `Bot.Api` interfaces:
  - Client/system: `DSharpSystem`, `DSharpClient`, `DiscordWrapper`.
  - Discord model wrappers: `DSharpGuild`, `DSharpMember`, `DSharpRole`, `DSharpChannel`, `DSharpMessage`, etc.
  - Builders/components: `DSharpEmbedBuilder`, `DSharpMessageBuilder`, `DSharpComponent`, `DSharpInteractionResponseBuilder`, etc.
  - Slash command modules: `DSharpGameSlashCommands`, `DSharpLookupSlashCommands`, `DSharpMessagingSlashCommands`, `DSharpMiscSlashCommands`, `DSharpSetupSlashCommands`.
- The solution includes local `DSharpPlus` projects and `Bot.DSharp` references them directly.
- Tests exist in `Test.Bot.DSharp`, so equivalent tests should be added or migrated.

## Target Design
- Add a new integration project such as `Bot.Remora` rather than rewriting `Bot.DSharp` in place. This keeps migration incremental and allows side-by-side verification.
- Implement the existing `Bot.Api` interfaces using Remora Discord types.
- Keep `Bot.Core` unchanged unless an interface gap requires a small, deliberate abstraction update.
- Update `Bot.Main` to register and run the Remora implementation when feature parity is reached.
- Remove DSharpPlus projects/references only after the Remora path passes tests and runtime verification.

## Implementation Steps
1. Create a new `Bot.Remora` project targeting `net10.0`.
2. Add Remora Discord package references needed for gateway, REST, commands, interactions, and dependency injection integration.
3. Add a `Bot.Remora` project reference to `Bot.Api` and `Bot.Base`.
4. Implement a `RemoraSystem : IBotSystem` equivalent to `DSharpSystem`:
   - Client creation.
   - Webhook builder creation.
   - Interaction response builder creation.
   - Embed and message builders.
   - Buttons, select menus, text inputs, and colors.
5. Implement Remora wrappers for the `Bot.Api.Discord` interfaces:
   - Guilds, channels, channel categories, members, roles, messages, embeds, colors, components, and interaction contexts.
6. Implement a Remora client equivalent to `DSharpClient`:
   - Read `DISCORD_TOKEN` and `DEPLOY_TYPE` through the existing environment/config abstraction.
   - Configure gateway intents equivalent to current DSharp intent usage.
   - Raise `Connected` and `MessageCreated` events expected by `IBotClient`.
   - Call `IComponentService` for component interactions.
   - Implement modal submission behavior, or preserve the current not-implemented behavior with an explicit issue if parity is not possible in the first pass.
7. Convert slash command modules to Remora command groups:
   - `game`, `night`, `day`, `vote`, `voteTimer`, `stopVoteTimer`, `endGame`, `storytellers`.
   - Lookup commands.
   - Messaging commands.
   - Misc commands.
   - Setup commands.
8. Recreate dev/prod command registration behavior:
   - In dev, register commands only for the configured development guild IDs.
   - In prod, register global commands and avoid leaving stale dev-guild commands for the production bot.
   - Move development guild IDs to configuration rather than hard-coding them if possible.
9. Add `Bot.Remora\ServiceFactory.cs` mirroring the existing DSharp service registration.
10. Add `Test.Bot.Remora` or migrate `Test.Bot.DSharp` tests to validate the new wrappers and command wiring.
11. Update `Bot.Main\Bot.Main.csproj` to reference `Bot.Remora`.
12. Update `Bot.Main\Program.cs` to use `Bot.Remora.ServiceFactory.RegisterServices(sp)` and `new RemoraSystem()`.
13. Run targeted Discord adapter tests, then the full solution build.
14. After runtime verification, remove `Bot.DSharp`, `Test.Bot.DSharp`, and local `DSharpPlus` project references if they are no longer needed.

## Progress Update (Phase 1)
Completed in this pass:
- Added central package versions for:
  - `Remora.Discord.API`
  - `Remora.Discord.Commands`
  - `Remora.Discord.Gateway`
  - `Remora.Discord.Interactivity`
  - `Remora.Discord.Rest`
- Created new `Bot.Remora` project targeting `net10.0` with references to `Bot.Api` and `Bot.Base`.
- Added initial scaffolding implementations:
  - `RemoraSystem`
  - `ServiceFactory`
  - placeholder Remora client/builders/components/colors needed to satisfy `Bot.Api` interfaces for incremental migration.
- Created `Test.Bot.Remora` with baseline tests for:
  - system construction/interface compliance
  - service registration wiring
  - missing token validation on client construction
- Added `Bot.Remora` and `Test.Bot.Remora` to `bot-on-the-clocktower.slnx`.
- Validation run:
  - `Test.Bot.Remora`: 4/4 passed
  - `Test.Bot.DSharp`: 24/24 passed
  - `Test.Main`: 3/3 passed
  - Full workspace build: successful

Status after this pass:
- ✅ Steps 1-3 completed.
- 🟡 Steps 4, 9, 10 scaffolded for incremental migration; functional parity still pending.
- ⏭️ Next focus: implement real Remora wrappers/client behavior and command registration parity (steps 5-8), then switch `Bot.Main` (steps 11-12).

## Progress Update (Phase 2)
Completed in this pass:
- Implemented adapter-level Remora wrappers for core `Bot.Api` Discord interfaces:
  - `RemoraGuild`, `RemoraChannel`, `RemoraChannelCategory`, `RemoraMember`, `RemoraRole`, `RemoraMessage`, `RemoraInteractionContext`.
- Expanded `RemoraClient` behavior for migration parity scaffolding:
  - validates `DISCORD_TOKEN`
  - validates `DEPLOY_TYPE` is `dev` or `prod`
  - tracks connection state and only emits `Connected` once per connection session
  - supports deterministic guild registration/lookup for testable behavior
  - supports non-bot message event raising via `MessageCreated`.
- Improved `RemoraSystem`/builder/component consistency:
  - component factories now create typed Remora components for button/select/text-input
  - webhook/interaction/message builders now enforce Remora type expectations for components/embeds.
- Expanded `Test.Bot.Remora` coverage:
  - deploy type validation
  - connect/event behavior
  - guild registration and lookup
  - wrapper contract/type-enforcement behavior.
- Validation run:
  - `Test.Bot.Remora`: 17/17 passed
  - `Test.Bot.DSharp`: 24/24 passed
  - `Test.Main`: 3/3 passed
  - Full workspace build: successful

Status after phase 2:
- ✅ Steps 1-5 partially/fully addressed at adapter level.
- 🟡 Steps 6-8 (live gateway events, component dispatch integration, Remora command groups and dev/prod registration parity) still pending.
- ⏭️ Next focus: wire real Remora gateway + interaction pipeline and port slash command modules.

## Progress Update (Phase 3)
Completed in this pass:
- Extended `RemoraClient` interaction behavior to integrate `IComponentService` dispatch.
- Added `DispatchComponentInteractionAsync(...)` to create a `RemoraInteractionContext` and forward interaction calls through `IComponentService.CallAsync(...)`.
- Added a safe no-op component service fallback when `IComponentService` is not registered, returning `false` for dispatch attempts.
- Expanded `Test.Bot.Remora` client coverage for interaction dispatch:
  - no component service → returns `false`
  - registered component service → propagates context and returns callback result.
- Expanded `RemoraInteractionContext` test coverage for happy-path behavior:
  - defer interaction response
  - edit response with webhook builder
  - update original message
  - show modal
  - preserved mismatch rejection tests.
- Validation run:
  - `Test.Bot.Remora`: 20/20 passed
  - `Test.Bot.DSharp`: 24/24 passed
  - `Test.Main`: 3/3 passed
  - Full workspace build: successful

Status after phase 3:
- ✅ Step 6 now partially addressed at adapter level (component interaction dispatch path is in place).
- 🟡 Steps 7-8 remain pending (Remora command groups and dev/prod command registration parity).
- ⏭️ Next focus: implement real Remora gateway event wiring + slash command conversion, then switch `Bot.Main` to `Bot.Remora`.

## Progress Update (Phase 4)
Completed in this pass:
- Added command registration policy scaffolding in `Bot.Remora`:
  - `RemoraCommandRegistrationPlan`
  - `RemoraCommandRegistrationPlanner`
- Implemented dev/prod command registration policy planning:
  - `dev`: no global command registration, register to dev guilds
  - `prod`: register globally and mark dev guild commands for cleanup
- Implemented dev guild ID source precedence in planner:
  1. `DISCORD_DEV_GUILD_IDS` (CSV)
  2. indexed keys (`Discord:DevGuildIds:0..15`)
  3. legacy default development guild IDs (for backward compatibility)
- Integrated planner into `RemoraClient`:
  - exposes `CommandRegistrationPlan` for testability and future command wiring
  - uses planner validation for deploy type semantics while preserving existing client exception shape.
- Expanded `Test.Bot.Remora` coverage for command planning policy:
  - dev/prod plan flags
  - CSV guild ID parsing
  - indexed guild ID parsing fallback.
- Validation run:
  - `Test.Bot.Remora`: 24/24 passed
  - `Test.Bot.DSharp`: 24/24 passed
  - `Test.Main`: 3/3 passed
  - Full workspace build: successful

Status after phase 4:
- ✅ Step 8 partially addressed with policy-level parity scaffolding.
- 🟡 Step 7 remains pending (actual Remora command group conversion and wiring).
- 🟡 Remaining major milestones: live Remora gateway/interaction command registration calls, `Bot.Main` cutover to `Bot.Remora`, and post-cutover DSharp cleanup.

## Progress Update (Phase 5)
Completed in this pass:
- Added command registration execution abstraction in `Bot.Remora`:
  - `IRemoraCommandRegistrar`
  - `NoOpRemoraCommandRegistrar`
- Integrated registrar execution in `RemoraClient.ConnectAsync`:
  - applies `CommandRegistrationPlan` on first connect only
  - `dev`: registers guild commands for plan guild IDs
  - `prod`: clears guild commands for plan guild IDs, then registers global commands
- Preserved existing connection/event semantics:
  - `Connected` remains one-time per connection session
  - registration execution is also one-time per connection session
- Expanded `Test.Bot.Remora` coverage for registration execution:
  - dev connect executes guild registration only (once)
  - prod connect executes clear-dev-guild + global registration
- Validation run:
  - `Test.Bot.Remora`: 24/24 passed
  - `Test.Bot.DSharp`: 24/24 passed
  - `Test.Main`: 3/3 passed
  - Full workspace build: successful

Status after phase 5:
- ✅ Step 8 further advanced: policy is now executed through a registrar abstraction.
- 🟡 Step 7 remains pending (actual Remora command group conversion and registration payload wiring).
- 🟡 Remaining milestones: real Remora gateway interaction wiring for commands, `Bot.Main` cutover to `Bot.Remora`, and post-cutover DSharp cleanup.

## Progress Update (Phase 6)
Completed in this pass:
- Updated `Bot.Main` to support dual integration host wiring (`Bot.DSharp` and `Bot.Remora`) without forcing a full cutover.
- Added `Bot.Remora` project reference to `Bot.Main` while retaining `Bot.DSharp`.
- Added `Program.ResolveDiscordIntegration(...)` helper with safe default behavior:
  - defaults to `dsharp`
  - switches to `remora` only when explicitly configured.
- Updated `Program` runtime service/system wiring:
  - reads `DISCORD_INTEGRATION`
  - registers either `DSharp.ServiceFactory` or `Remora.ServiceFactory`
  - runs `DSharpSystem` or `RemoraSystem` accordingly.
- Added Remora log destructuring entries in `Program` to keep runtime logging stable when Remora types are active.
- Expanded `Test.Main` with integration-selection tests covering null/empty/mixed-case/unknown values and explicit `remora` selection.
- Validation run:
  - `Test.Main`: 10/10 passed
  - `Test.Bot.Remora`: 24/24 passed
  - `Test.Bot.DSharp`: 24/24 passed
  - Full workspace build: successful

Status after phase 6:
- ✅ Step 11 partially addressed (Bot.Main now references Bot.Remora and can select it via configuration).
- 🟡 Step 12 partially addressed (Program can run Remora path, but Remora command conversion/runtime parity still incomplete).
- 🟡 Remaining milestones: command group conversion (step 7), full registration/runtime parity (step 8), and final default cutover/cleanup.

## Progress Update (Phase 7)
Completed in this pass:
- Introduced an integration-neutral slash command abstraction in `Bot.Remora`:
  - `IRemoraSlashCommand` (name, description, parameters, invoke)
  - `RemoraSlashCommandParameter` and `RemoraSlashCommandParameterType`
  - `IRemoraSlashCommandSource` aggregator interface
  - `RemoraSlashCommandRegistry` to discover sources via DI
  - `RemoraSlashCommandArgumentExtensions` helpers for argument resolution
- Ported all five DSharp slash command modules to Remora equivalents with full command/parameter parity:
  - `RemoraGameSlashCommands` (`game`, `night`, `day`, `vote`, `voteTimer`, `stopVoteTimer`, `endGame`, `storytellers`)
  - `RemoraLookupSlashCommands` (`lookup`, `addScript`, `removeScript`, `listScripts`, `refreshScripts`)
  - `RemoraMessagingSlashCommands` (`evil`, `lunatic`)
  - `RemoraMiscSlashCommands` (`announce`)
  - `RemoraSetupSlashCommands` (`createTown`, `townInfo`, `destroyTown`, `modifyTown`, `addTown`, `removeTown`)
- Registered all five command sources via `Bot.Remora/ServiceFactory.cs` into a shared `RemoraSlashCommandRegistry`.
- Added `Test.Bot.Remora/TestSlashCommands.cs` covering:
  - Command name parity per source
  - Argument routing for representative commands (`game`, `voteTimer`, `storytellers`, `addScript`, `evil`, `announce`, `createTown`)
  - `ServiceFactory` registers all expected slash command sources.
- Validation run:
  - All projects: 71/71 tests passed (Test.Bot.Remora, Test.Bot.DSharp, Test.Main)
  - Full workspace build: successful

Status after phase 7:
- ✅ Step 7 addressed at adapter level: the full DSharp command surface now has a Remora equivalent expressed in an integration-neutral form.
- 🟡 Live Remora gateway/interaction wiring for executing these commands and the `IRemoraCommandRegistrar` payload still pending (step 8 finish).
- 🟡 Final default cutover and DSharp cleanup remain pending.

## Progress Update (Phase 8)
Completed in this pass:
- Completed step 8 wiring from policy execution to payload-aware command registration in `Bot.Remora`.
- Updated `IRemoraCommandRegistrar` contract so registration methods receive the command payload set:
  - `RegisterGuildCommandsAsync(IReadOnlyCollection<ulong>, IReadOnlyCollection<IRemoraSlashCommand>)`
  - `RegisterGlobalCommandsAsync(IReadOnlyCollection<IRemoraSlashCommand>)`
- Updated `RemoraClient` registration execution to resolve slash commands from `RemoraSlashCommandRegistry` and pass those commands to registrar calls during `ConnectAsync`.
- Added safe fallback behavior in `RemoraClient`: if no `RemoraSlashCommandRegistry` is registered, an empty command set is passed.
- Updated `RemoraSlashCommandRegistry` to support factory-based source registration (`AddSource(Func<IServiceProvider, T>)`) so command sources can be created using runtime DI dependencies.
- Updated `Bot.Remora/ServiceFactory.cs` to register all slash command sources with dependency-aware factory delegates.
- Expanded tests:
  - `Test.Bot.Remora/TestClient.cs` now verifies dev/prod registrar calls include resolved command payloads.
  - `Test.Bot.Remora/TestSlashCommands.cs` now verifies registry command resolution via factory injection.
- Validation run:
  - Full build: successful
  - Targeted suite: 72/72 passed (`Test.Bot.Remora`, `Test.Bot.DSharp`, `Test.Main`)

Status after phase 8:
- ✅ Step 8 completed at adapter and registration-payload wiring level.
- 🟡 Remaining work for full Remora cutover is operational/runtime integration beyond this planning step (live gateway execution verification, default integration flip, and eventual DSharp cleanup).

## Progress Update (Phase 9-13)
Completed in this pass:
- Step 9 completed: finalized `Bot.Remora/ServiceFactory.cs` runtime registration by explicitly adding
  - `IRemoraCommandRegistrar` -> `NoOpRemoraCommandRegistrar`
  - existing `RemoraSlashCommandRegistry` command-source registration retained.
- Step 10 completed (for current Remora scope): expanded `Test.Bot.Remora/TestServices.cs` to assert all key Remora registrations:
  - `IColorBuilder` -> `RemoraColorBuilder`
  - `IRemoraCommandRegistrar` -> `NoOpRemoraCommandRegistrar`
  - `RemoraSlashCommandRegistry` registration.
- Step 11 completed: `Bot.Main/Bot.Main.csproj` now references `Bot.Remora` for primary host path and no longer references `Bot.DSharp`.
- Step 12 completed: `Bot.Main/Program.cs` cut over to Remora runtime wiring:
  - removed dual integration selection and `ResolveDiscordIntegration(...)`
  - registers `Remora.ServiceFactory` directly
  - runs `new RemoraSystem()` directly
  - removed DSharp-specific log destructuring entries.
- Step 13 completed: validation run after cutover
  - Full build: successful
  - Targeted tests: 67/67 passed (`Test.Bot.Remora`, `Test.Bot.DSharp`, `Test.Main`).

Status after phase 9-13:
- ✅ Main host is now cut over to Remora by default and runtime wiring is simplified.
- ✅ Remora registration path includes default registrar + command registry wiring.
- 🟡 Step 14 remains pending by design: remove DSharp integration/projects/tests and local DSharpPlus references after additional runtime verification.

## Progress Update (Phase 14)
Completed in this pass:
- Removed legacy DSharp and local DSharpPlus projects from the solution manifest (`bot-on-the-clocktower.slnx`):
  - `Bot.DSharp/Bot.DSharp.csproj`
  - `Test.Bot.DSharp/Test.Bot.DSharp.csproj`
  - `DSharpPlus/DSharpPlus/DSharpPlus.csproj`
  - `DSharpPlus/DSharpPlus.SlashCommands/DSharpPlus.SlashCommands.csproj`
- Removed the corresponding project files from disk so they are no longer part of the active workspace build graph.
- Preserved Remora-based runtime host wiring and tests introduced in phases 9-13.
- Validation run after cleanup:
  - Full build: successful
  - Targeted tests: 52/52 passed (`Test.Bot.Remora`, `Test.Main`, `Test.Bot.Database`).

Status after phase 14:
- ✅ Legacy DSharp integration and local DSharpPlus project references are removed from the active solution path.
- ✅ Main host remains Remora-only and build/test validation is green.
- 🟡 Optional follow-up cleanup (non-blocking): remove leftover source directories if desired after archive/backup decisions.

## Configuration Changes
- Preserve existing keys initially:
  - `DISCORD_TOKEN`
  - `DEPLOY_TYPE`
- Prefer moving hard-coded dev guild IDs into configuration, for example:
  - `Discord:DevGuildIds:0`
  - `Discord:DevGuildIds:1`
- If Remora needs additional settings, place non-secret defaults in `appsettings.json` and secrets in user secrets/environment variables.

## Validation Checklist
- Bot connects and emits the `Connected` event.
- Non-bot message creation still triggers `IBotClient.MessageCreated`.
- Slash commands are registered correctly in dev and prod modes.
- All command handlers still receive an `IBotInteractionContext` compatible with `Bot.Core`.
- Buttons/select menus still call `IComponentService`.
- Embeds, messages, colors, roles, members, and channels behave the same from `Bot.Core`'s perspective.
- Full solution builds without DSharpPlus references once cleanup is complete.

## Risks and Open Questions
- Remora's interaction and command model differs from DSharpPlus, so command registration and dependency injection may need more structural changes than the wrapper types.
- Some DSharpPlus model properties may not have direct Remora equivalents; wrapper interfaces may need careful mapping.
- Current code has nullable annotations despite workspace guidance discouraging new nullable usage; avoid expanding nullable usage while preserving existing contracts unless a broader cleanup is approved.
- Discord command propagation timing can make runtime validation slower than unit validation.
