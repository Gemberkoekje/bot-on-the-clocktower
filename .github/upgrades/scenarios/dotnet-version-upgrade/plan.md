# .NET Version Upgrade Plan

## Overview

**Target**: Upgrade all 11 projects from `net6.0` to `net10.0`
**Scope**: Medium solution — 11 projects (5 class libraries, 1 console app, 5 test projects), 3-tier dependency graph, all SDK-style

### Selected Strategy
**All-At-Once** — All projects upgraded simultaneously in a single operation.
**Rationale**: 11 projects, all on net6.0, 3-tier dependency depth — atomic upgrade minimises overhead with no multi-targeting needed.

## Tasks

### 01-prerequisites: Verify SDK and toolchain readiness

Confirm that the .NET 10 SDK is installed and that any `global.json` files in the repository are compatible with the target SDK. This is a non-destructive verification step that must pass before any project file changes are made.

The repository is currently on net6.0. Any `global.json` pinning an SDK version older than the .NET 10 range will need to be updated so the toolchain resolves correctly after the TFM bump.

**Done when**: .NET 10 SDK is confirmed installed; any `global.json` in the repo is updated to allow .NET 10 SDK resolution; no build or restore failures caused by toolchain mismatch.

---

### 02-cpm-setup: Set up Central Package Management

Create `Directory.Packages.props` at the repository root and migrate all `PackageReference` version attributes out of the individual `.csproj` files. This makes package versions consistent and centrally managed across all 11 projects before the version bump happens.

All projects are already SDK-style and on the same ecosystem, making this the right moment to introduce CPM cleanly. The current packages span 13 distinct entries across the solution. After this task, `.csproj` files should contain `<PackageReference Include="..." />` entries without `Version` attributes, and all versions are declared in `Directory.Packages.props`.

**Done when**: `Directory.Packages.props` exists at the repo root with all package versions declared; all `.csproj` files have `PackageReference` entries without `Version` attributes; `dotnet restore` succeeds and the solution builds on net6.0 (no regression).

---

### 03-upgrade-projects: Upgrade all projects to net10.0

Update the `TargetFramework` in all 11 project files from `net6.0` to `net10.0`, update package versions in `Directory.Packages.props` (including the `MongoDB.Driver` security fix from 2.15.0 → 3.8.1 and `Newtonsoft.Json` from 13.0.1 → 13.0.4), and resolve all source-incompatible API usages and behavioral changes flagged by the assessment.

The assessment flagged source-incompatible APIs (`Api.0002`) in `Bot.Api`, `Bot.Core`, `Bot.Database`, and `Test.Bot.Core` (the bulk of the 58 issues in `Test.Bot.Core`), and behavioral changes (`Api.0003`) in `Bot.Core`, `Bot.Main`, and `Test.Bot.Core`. All API issues are to be fixed inline — no stubs or deferral. The `MongoDB.Driver` upgrade from 2.x to 3.x is a major version bump and may require driver API adjustments in `Bot.Database` and its tests.

Projects to update (in dependency order for reference, though all are changed together):
- **Level 0**: `Bot.Api`, `Bot.Base`
- **Level 1**: `Bot.Core`, `Bot.Database`, `Bot.DSharp`, `Test.Bot.Base`
- **Level 2**: `Bot.Main`, `Test.Bot.Core`, `Test.Bot.Database`, `Test.Bot.DSharp`
- **Level 3**: `Test.Main`

**Done when**: All 11 projects target `net10.0`; `MongoDB.Driver` is at 3.8.1 and `Newtonsoft.Json` at 13.0.4; solution builds with 0 errors and 0 warnings; all source-incompatible API usages and behavioral change sites are resolved.

---

### 04-final-validation: Validate full solution and document deferred items

Run the full test suite across all test projects (`Test.Bot.Base`, `Test.Bot.Core`, `Test.Bot.Database`, `Test.Bot.DSharp`, `Test.Main`) and confirm all tests pass. Document any deferred recommendations — in particular, a note that Central Package Management is now in place and Nullable Reference Types can be enabled as a follow-up effort.

**Done when**: All tests pass; solution builds cleanly with 0 errors and 0 warnings; a brief summary of deferred recommendations (nullable reference types enablement) is recorded in `progress-details.md`.
