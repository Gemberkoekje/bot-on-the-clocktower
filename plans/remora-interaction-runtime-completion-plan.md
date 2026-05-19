# Remora Interaction Runtime Completion Plan

## Goal
Implement the missing runtime path so registered slash commands are actually executed and acknowledged in Discord (no more "The application did not respond").

## Current Gaps (Confirmed)
1. Slash command registration works, but incoming interactions are not dispatched to `IRemoraSlashCommand` handlers.
2. No gateway responder is wired to process `InteractionCreate` events.
3. Current `Bot.Remora` wrappers (`RemoraGuild`, `RemoraChannel`, `RemoraMember`, etc.) are mostly in-memory stubs and do not map to live Discord state/API operations.
4. Interaction response lifecycle (defer, edit original response, modal/update flows) is not implemented against Discord REST endpoints.
5. Argument binding from Discord interaction options to handler argument types (`string`, `bool`, `IMember`, `IRole`, `IChannel`, `IChannelCategory`) is incomplete.

## Scope
- Complete command execution for slash interactions.
- Complete component interaction execution path.
- Keep existing command surface (`IRemoraSlashCommand`) and registration plan behavior (`dev`/`prod`).
- Add robust diagnostics and tests for all runtime interaction plumbing.

## Non-Goals
- Reworking command definitions in `Remora*SlashCommands`.
- Changing deployment model (`dev` guild registration vs `prod` global registration).
- Broad refactor outside `Bot.Remora` and required integration points.

## Design Overview

### 1) Add a gateway interaction responder
Implement a responder that listens for interaction gateway events and routes:
- slash commands -> command dispatcher
- component interactions -> `IComponentService`

Responder responsibilities:
- Identify interaction type.
- Build a real `IBotInteractionContext` backed by interaction token, IDs, and REST clients.
- Invoke the corresponding command/component callback.
- Ensure an ACK/defer is always sent within Discord timeout requirements.
- Return success/failure results with structured logging.

### 2) Add slash command dispatcher
Implement a service that:
- builds a normalized command lookup from `RemoraSlashCommandRegistry.ResolveCommands()`;
- maps interaction command names to `IRemoraSlashCommand`;
- parses options recursively (including sub-option shapes if present);
- resolves typed values from interaction `Resolved` payload:
  - users/members -> `IMember`
  - roles -> `IRole`
  - channels -> `IChannel` / `IChannelCategory` (when applicable)
- invokes `InvokeAsync(context, arguments)`.

### 3) Implement real interaction context + response operations
Replace stub-like behavior with Discord-backed operations:
- `DeferInteractionResponse()` -> send interaction callback (deferred type).
- `EditResponseAsync(...)` -> edit original interaction response via webhook endpoints.
- `UpdateOriginalMessageAsync(...)` for component updates.
- `ShowModalAsync(...)` using modal callback response.

Maintain state flags (`IsDeferred`) and idempotency guardrails.

### 4) Implement live Discord entity adapters
Add/upgrade wrappers that represent real Discord entities and operations:
- guild: role/channel lookup and creation via REST
- channel: send messages, overwrite management, deletes
- member: move voice, role grant/revoke, nicknames, DM
- role/category types where required

Adapters should only expose capabilities needed by `Bot.Api` interfaces.

### 5) Wire DI and gateway responder registration
In `AddRemoraServices`:
- register dispatcher + runtime adapter services;
- register responder using gateway responder extension;
- ensure required REST abstractions are available;
- ensure responder is instantiated with command registry + services.

### 6) Observability and failure handling
Add logs for:
- incoming interaction type, command name, guild/channel/member IDs;
- argument binding failures and missing resolved entities;
- ACK/defer/send/edit operations and REST failures;
- command execution duration and terminal status.

Add fallback user-visible ephemeral error response when command execution fails before reply.

## Implementation Steps
1. **Inventory runtime touchpoints**
   - Confirm exact Remora event and REST APIs used for interaction callback/edit/followup.
2. **Create interaction responder**
   - Add `IResponder<...InteractionCreate...>` implementation and basic routing skeleton.
3. **Create slash dispatch service**
   - Add command map, command lookup, and option extraction/binding pipeline.
4. **Build live interaction context**
   - Implement defer/edit/update/modal against Discord interaction endpoints.
5. **Implement/upgrade entity adapters**
   - Replace in-memory stubs with REST-backed behavior needed by handlers.
6. **Implement component interaction routing**
   - Map component custom IDs/values and invoke `IComponentService`.
7. **DI wiring**
   - Register responder + dispatcher + supporting services in `DependencyInjection`.
8. **Error/timeout hardening**
   - Ensure guaranteed ACK path and graceful error responses.
9. **Unit tests (Bot.Remora)**
   - Add tests for dispatch routing, argument binding, and failure modes.
10. **Integration-style tests (targeted)**
   - Validate command callback invocation from synthetic interaction payloads.
11. **Manual verification checklist**
   - dev mode guild command invocation;
   - prod mode global invocation;
   - component callback behavior.
12. **Documentation updates**
   - Update `ARCHITECTURE.md` and `README.md` with runtime interaction behavior + troubleshooting.

## File Targets (Expected)
- `Bot.Remora/DependencyInjection.cs`
- `Bot.Remora/RemoraClient.cs` (only if lifecycle hooks need adjustment)
- `Bot.Remora/RemoraInteractionContext.cs`
- `Bot.Remora/RemoraInteractionResponseBuilder.cs` (if needed)
- `Bot.Remora/RemoraChannel.cs`
- `Bot.Remora/RemoraGuild.cs`
- `Bot.Remora/RemoraMember.cs`
- `Bot.Remora/RemoraRole.cs`
- `Bot.Remora/*Responder*.cs` (new)
- `Bot.Remora/*Dispatcher*.cs` (new)
- `Test.Bot.Remora/*` (new/updated tests)
- `README.md`
- `ARCHITECTURE.md`

## Validation Checklist
- Build succeeds.
- Existing Remora tests pass.
- New dispatch tests pass.
- Slash commands return responses instead of timeout.
- `/createtown` and one gameplay command execute successfully in a real guild.
- Logs clearly show:
  - deploy mode,
  - registration target(s),
  - incoming interaction routing,
  - response lifecycle operations.

## Risks
- Remora API surface differences across package versions may require adapter shims.
- Discord interaction timing constraints can cause intermittent timeouts if defer path is delayed.
- Entity resolution complexity (member/role/channel mapping) can create edge-case failures in large guilds.

## Rollout Recommendation
1. Implement and validate in `dev` mode with one known guild ID.
2. Run command smoke tests (`/createtown`, `/game`, `/vote`, `/towninfo`).
3. Promote to `prod` once interaction execution and logs are stable.
