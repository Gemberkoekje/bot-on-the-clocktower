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
