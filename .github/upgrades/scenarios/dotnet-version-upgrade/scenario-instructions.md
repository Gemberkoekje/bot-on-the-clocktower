# .NET Version Upgrade

## Preferences
- **Flow Mode**: Automatic
- **Target Framework**: net10.0

## Source Control
- **Source Branch**: main
- **Working Branch**: upgrade-dotnet-10
- **Commit Strategy**: After Each Task

## Strategy
**Selected**: All-at-Once
**Rationale**: 11 projects all on net6.0, 3-tier graph, no .NET Framework projects — atomic upgrade is fastest with no multi-targeting overhead.

### Execution Constraints
- All projects are upgraded together in a single atomic pass — no tier ordering required
- Fix all API issues inline (no stubs or deferral)
- Validate full solution build (0 errors, 0 warnings) before marking upgrade task complete
- Run full test suite only after the atomic upgrade task completes successfully
- CPM is set up as a prerequisite task before the TFM bump

## Upgrade Options
**Source**: .github/upgrades/scenarios/dotnet-version-upgrade/upgrade-options.md

### Strategy
- Upgrade Strategy: All-at-Once

### Project Structure
- Package Management: Central Package Management (CPM)

### Compatibility
- Unsupported API Handling: Fix Inline

### Modernization
- Nullable Reference Types: Leave Disabled
