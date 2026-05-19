# Remora Interaction Runtime Completion — Phased Plan

## Background

Slash command registration works, but incoming interactions are not dispatched to `IRemoraSlashCommand` handlers, producing "The application did not respond" for every command. This plan completes the missing runtime path incrementally through nine reviewable, individually-shippable phases.

### Confirmed gaps
1. No gateway responder wired to process `InteractionCreate` events.
2. Incoming interactions are never dispatched to `IRemoraSlashCommand` handlers.
3. `RemoraGuild`, `RemoraChannel`, `RemoraMember`, etc. are in-memory stubs with no REST backing.
4. Interaction response lifecycle (defer → edit → modal/update) is not implemented.
5. Argument binding from Discord options to handler types is incomplete.

---

## Guiding principles

- One PR per phase. Each phase must build green, pass existing tests, and add its own unit tests.
- Never break the existing command surface (`IRemoraSlashCommand`) or the registration plan (`dev`/`prod`).
- New code lives behind interfaces inside `Bot.Remora`; no changes to `Bot.Core`/`Bot.Api`.
- Each phase ends with structured logs that make the next phase debuggable.
- No nullables: prefer `Result`/sentinel patterns.
- Implicit usings are disabled — every new file must declare `System.*` usings explicitly.
- Warnings must be resolved per phase; suppress via `.globalconfig` only with a justification comment.
- DI style: explicit constructor injection throughout.

---

## Phase dependency graph

```
Phase 0 → Phase 1 → Phase 2
                  → Phase 3
                  → Phase 4 → Phase 5 → Phase 6 → Phase 7 → Phase 8
```

---

## ✅ Phase 0 — Runtime inventory & DI seam
**Status: Complete** (commit `81770f4`)

**Goal:** Land scaffolding that lets later phases plug in without churn, and confirm exactly which Remora APIs will be used.

**Delivered:**
- Architecture inventory added to `ARCHITECTURE.md`:
  - Gateway responder surface: `IResponder<TGatewayEvent>`, concrete event `InteractionCreate`.
  - Interaction REST surface: `IDiscordRestInteractionAPI` — `CreateInteractionResponseAsync`, `EditOriginalInteractionResponseAsync`, `CreateFollowupMessageAsync`.
  - Webhook REST surface: `IDiscordRestWebhookAPI` — `EditWebhookMessageAsync`, `ExecuteWebhookAsync`.
- Internal runtime seam interfaces added to `Bot.Remora/RemoraInteractionRuntimeSeams.cs`:
  - `IRemoraInteractionResponder`
  - `IRemoraSlashCommandDispatcher`
  - `IRemoraComponentDispatcher`
- No-op implementations registered in `Bot.Remora/DependencyInjection.cs` (emit debug-level "not yet wired" messages; behavior unchanged).
- `Bot.Remora/Properties/AssemblyInfo.cs` with `InternalsVisibleTo("Test.Bot.Remora")`.
- `Test.Bot.Remora/TestServices.cs` smoke test: DI resolves all three seam types.

**Files changed:**
- `ARCHITECTURE.md`
- `Bot.Remora/DependencyInjection.cs`
- `Bot.Remora/RemoraInteractionRuntimeSeams.cs` *(new)*
- `Bot.Remora/Properties/AssemblyInfo.cs` *(new)*
- `Test.Bot.Remora/TestServices.cs`

---

## ✅ Phase 1 — Slash command dispatch for primitive arguments
**Status: Complete (this branch)**

**Goal:** Make registered slash commands actually execute and reply (fixes "did not respond").

**Includes:**
- Real `IRemoraInteractionResponder` registered as a Remora `IResponder<InteractionCreate>` (slash subtype only).
- Real `IRemoraSlashCommandDispatcher`:
  - Name → `IRemoraSlashCommand` map from `RemoraSlashCommandRegistry.ResolveCommands()`.
  - Primitive argument binding: `string`, `bool`, `int`/`long` options.
  - Entity parameters skipped (logged + absent; affected commands get a friendly ephemeral message).
- Live interaction context (`LiveRemoraInteractionContext` or upgraded `RemoraInteractionContext`) backed by REST:
  - `DeferInteractionResponse()` → `DeferredChannelMessageWithSource` callback.
  - `EditResponseAsync(...)` → `EditOriginalInteractionResponseAsync` via webhook.
  - `UpdateOriginalMessageAsync`/`ShowModalAsync` → stub `NotSupportedException` until Phase 3.
- Guaranteed ACK path: defer always sent within 3-second budget; ephemeral error if dispatch throws first.
- Structured logging: interaction type, command name, guild/channel/member IDs, timing, terminal status.
- DI wiring: replace no-op implementations with real ones in `AddRemoraServices`.

**Tests (`Test.Bot.Remora`):**
- Dispatcher: command name resolution (case sensitivity, unknown command).
- Dispatcher: primitive option binding (present, missing, wrong type).
- Responder: routes slash → dispatcher; ignores components (logs).
- Context: defer→edit lifecycle calls correct REST abstraction (mocked).
- Failure path: handler throws → ephemeral error sent, no unhandled exception escapes.

**Acceptance:** Build + tests green. A string/bool-arg command (e.g. `/announce`) runs end-to-end in `dev` mode.

**Delivered:**
- Real `IRemoraInteractionResponder` and `IRemoraSlashCommandDispatcher` implementations added.
- `RemoraGatewayInteractionResponder : IResponder<InteractionCreate>` added and wired via DI.
- Primitive argument binding implemented for `string`, `bool`, and integer-style option values.
- Entity-parameter commands are detected in Phase 1 and return a friendly ephemeral follow-up message.
- `LiveRemoraInteractionContext` added with live defer/edit REST behavior (`CreateInteractionResponseAsync`, `EditOriginalInteractionResponseAsync`).
- `UpdateOriginalMessageAsync` and `ShowModalAsync` intentionally throw `NotSupportedException` pending Phase 3.
- Failure path hardened: responder catches dispatch exceptions and sends an ephemeral error response without rethrowing.
- Added focused `Test.Bot.Remora` coverage for dispatcher routing/binding, responder behavior, context defer/edit lifecycle, and failure handling.

**Files changed:**
- `Bot.Remora/DependencyInjection.cs`
- `Bot.Remora/IRemoraSlashCommand.cs`
- `Bot.Remora/LiveRemoraInteractionContext.cs` *(new)*
- `Bot.Remora/RemoraCommandRegistrar.cs`
- `Bot.Remora/RemoraGatewayInteractionResponder.cs` *(new)*
- `Bot.Remora/RemoraInteractionResponder.cs` *(new)*
- `Bot.Remora/RemoraInteractionRuntimeSeams.cs`
- `Bot.Remora/RemoraSlashCommandDispatcher.cs` *(new)*
- `Test.Bot.Remora/TestInteractionRuntime.cs` *(new)*

---

## ✅ Phase 2 — Entity argument binding
**Status: Complete (this branch)**

**Goal:** Resolve `IMember`, `IRole`, `IChannel`, `IChannelCategory` from the interaction `Resolved` payload.

**Includes:**
- Extend the dispatcher binder to map option values + `Resolved` to `Bot.Api` interface types.
- Read-only resolved adapters: `ResolvedMemberAdapter`, `ResolvedRoleAdapter`, `ResolvedChannelAdapter`, `ResolvedChannelCategoryAdapter` (mutating ops throw `NotSupportedException`).
- Channel type 4 detection → route to `IChannelCategory` adapter.
- Additive `RemoraSlashCommandParameterType` enum value if needed to distinguish channel vs. category.

**Tests:** Binder per entity type (present, missing, wrong type, optional). Mutating adapters throw documented exception.

**Acceptance:** Build + tests green. A member/role/channel command runs end-to-end without cast failures.

**Delivered:**
- `RemoraSlashCommandDispatcher` now binds `User`, `Role`, and `Channel` parameters from `IApplicationCommandData.Resolved`.
- Added read-only resolved adapters:
  - `ResolvedMemberAdapter`
  - `ResolvedRoleAdapter`
  - `ResolvedChannelAdapter`
  - `ResolvedChannelCategoryAdapter`
- Channel category routing implemented via resolved `ChannelType.GuildCategory` (Discord type 4), allowing `IChannelCategory` arguments to bind correctly from channel options.
- Mutating operations on resolved adapters now throw `NotSupportedException` with a consistent phase-2 message.
- Added focused runtime tests for:
  - Entity binding success path and optional arguments.
  - Missing/invalid resolved entity payload failure cases.
  - Read-only adapter mutation guards.

**Files changed:**
- `Bot.Remora/RemoraSlashCommandDispatcher.cs`
- `Bot.Remora/ResolvedRemoraAdapters.cs` *(new)*
- `Test.Bot.Remora/TestInteractionRuntime.cs`

---

## ✅ Phase 3 — Component interactions & modal flows
**Status: Complete (this branch)**

**Goal:** Route button/select/modal interactions through `IComponentService` and finish the context lifecycle.

**Includes:**
- Responder extended for component and modal interaction subtypes.
- Real `IRemoraComponentDispatcher`: resolve custom IDs + values, build context, invoke `IComponentService`.
- Finish `UpdateOriginalMessageAsync` (component update) and `ShowModalAsync` (modal response) against REST.
- `IsDeferred` idempotency: double defer is a no-op; edit before defer auto-defers.
- Logging: custom IDs, modal submission keys.

**Tests:** Component dispatch (known/unknown IDs), modal show + submission (mocked REST), idempotency rules.

**Acceptance:** Build + tests green. A button-driven flow round-trips in `dev` mode.

**Delivered:**
- Responder routing expanded to handle `InteractionType.MessageComponent` and `InteractionType.ModalSubmit`, with graceful fallback ephemeral responses when a component ID is unknown.
- Added real `RemoraComponentDispatcher` implementation:
  - Parses interaction payloads for message component and modal submit types.
  - Resolves custom IDs and submission values (including modal text input extraction).
  - Builds `LiveRemoraInteractionContext` and dispatches through `IComponentService`.
  - Emits structured logs including custom ID and modal submission keys.
- `LiveRemoraInteractionContext` now fully supports interaction lifecycle completion:
  - `UpdateOriginalMessageAsync(...)` implemented using `InteractionCallbackType.UpdateMessage`.
  - `ShowModalAsync(...)` implemented using `InteractionCallbackType.Modal`.
  - `DeferInteractionResponse()` remains idempotent.
  - `EditResponseAsync(...)` now auto-defers when called before explicit defer.
- Added focused `Test.Bot.Remora` coverage for:
  - Responder routing of slash/component/modal interactions.
  - Unknown component fallback response behavior.
  - Component dispatcher known/unknown custom ID behavior.
  - Modal submission value dispatch.
  - Live context defer idempotency, edit-before-defer auto-defer, update-message callback, and modal callback flows.

**Files changed:**
- `Bot.Remora/DependencyInjection.cs`
- `Bot.Remora/LiveRemoraInteractionContext.cs`
- `Bot.Remora/RemoraComponentDispatcher.cs` *(new)*
- `Bot.Remora/RemoraInteractionResponder.cs`
- `Bot.Remora/RemoraInteractionResponseBuilder.cs`
- `Bot.Remora/RemoraInteractionRuntimeSeams.cs`
- `Test.Bot.Remora/TestInteractionRuntime.cs`
- `Test.Bot.Remora/TestServices.cs`

---

## ⬜ Phase 4 — Live `RemoraGuild` adapter (read paths)
**Status: Pending** (requires Phase 1)

**Goal:** Replace in-memory stubs in `RemoraGuild` with REST-backed reads.

**Includes:**
- Inject REST guild API abstraction into `RemoraGuild`.
- Implement read methods used by `Bot.Core` (enumerate channels/roles/members, find by ID/name).
- Keep write methods throwing `NotSupportedException` until Phase 5.
- Cache only if `Bot.Core` requires snapshot semantics; otherwise leave caching out and document.

**Tests:** Each read method against mocked REST (happy path, not-found, paging).

**Acceptance:** Build + tests green. Read-only commands (e.g. `/townInfo`) work end-to-end.

---

## ⬜ Phase 5 — Live `RemoraChannel` write operations
**Status: Pending** (requires Phase 4)

**Goal:** REST-backed channel writes (send messages, permission overwrites, create/delete).

**Includes:**
- Implement `IChannel` methods consumed by `Bot.Core`, one at a time, each behind unit tests with mocked REST.
- Respect rate limits via what Remora already provides.
- Preserve `IChannel` method signatures exactly.

**Tests:** Per-method REST call assertions, error mapping (permissions, not-found).

**Acceptance:** Build + tests green. `/createTown` succeeds in `dev` mode.

---

## ⬜ Phase 6 — Live `RemoraMember` operations
**Status: Pending** (requires Phase 5)

**Goal:** REST-backed member writes: voice move, role grant/revoke, nickname, DM.

**Includes:**
- Implement each `IMember` mutating method against the REST member API.
- DM channel acquisition via REST + message send.
- Map Discord error codes (missing permissions, not in voice) to existing `Bot.Api` error semantics — no new exception types.

**Tests:** Per-method REST call assertions and error mapping.

**Acceptance:** Build + tests green. A voice-movement command (e.g. `/night`) works in `dev` mode.

---

## ⬜ Phase 7 — Live `RemoraRole` / `RemoraChannelCategory` & loose ends
**Status: Pending** (requires Phase 6)

**Goal:** Finish remaining adapter operations and remove `NotSupportedException` stubs from hot paths.

**Includes:**
- Role create/delete/edit if consumed by `Bot.Core`; otherwise leave read-only and document.
- Category operations used by setup commands.
- Decide whether Phase 2 resolved adapters are replaced by live adapters or kept as a dual model; document explicitly in `ARCHITECTURE.md`.

**Tests:** Coverage for each newly live method.

**Acceptance:** No `NotSupportedException` on hot command paths. `/createTown`, `/game`, `/vote`, `/townInfo` all work in `dev` mode.

---

## ⬜ Phase 8 — Hardening, observability, documentation
**Status: Pending** (requires Phase 7)

**Goal:** Production-readiness pass; promote from `dev` to `prod`.

**Includes:**
- Timeout/cancellation review across responder and dispatcher.
- Consistent structured logging fields (correlation ID per interaction).
- Metrics counters if the project already uses any (no new dependency).
- `ARCHITECTURE.md`: runtime interaction lifecycle diagram in prose.
- `README.md`: troubleshooting section ("did not respond", missing permissions, dev vs prod registration).
- Confirm `prod` global registration path still works.
- Smoke-test checklist in PR description, executed by the maintainer against a real guild.

**Acceptance:** Docs landed. Maintainer signs off on live smoke tests.

---

## Manual validation note

The sandbox cannot exercise a live Discord guild. Every phase's "end-to-end in `dev` mode" acceptance criterion must be executed by a maintainer before merge. Unit tests in `Test.Bot.Remora` are the automated gate.
