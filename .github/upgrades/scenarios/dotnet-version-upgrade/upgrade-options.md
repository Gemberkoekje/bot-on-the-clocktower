# Upgrade Options — bot-on-the-clocktower

Assessment: 11 projects, all net6.0 → net10.0, SDK-style, 3-tier dependency graph, 1 security vulnerability (MongoDB.Driver), source-incompatible APIs in 4 projects

## Strategy

### Upgrade Strategy
All 11 projects are already on modern .NET (net6.0), the graph is shallow (3 tiers), and there are no .NET Framework projects — an atomic upgrade minimises overhead.

| Value | Description |
|-------|-------------|
| **All-at-Once** (selected) | Upgrade all projects simultaneously in a single atomic pass — fastest, no multi-targeting overhead. |
| Top-Down | Upgrade entry-point apps first, multi-target libraries temporarily; preferred for 15+ projects or CI-green constraints. |

## Project Structure

### Package Management
11 projects are all SDK-style, upgrading within the same modern ecosystem — CPM eliminates version drift across projects cleanly.

| Value | Description |
|-------|-------------|
| **Central Package Management (CPM)** (selected) | Create `Directory.Packages.props`, move all versions out of project files for consistent management. |
| Per-Project (defer CPM to post-migration) | Each project keeps its own versions; CPM added as a post-migration step. |

## Compatibility

### Unsupported API Handling
4 projects have source-incompatible APIs flagged (Bot.Api, Bot.Core, Bot.Database, Test.Bot.Core); this is a modern-to-modern upgrade so changes are expected to be minor and fixable inline.

| Value | Description |
|-------|-------------|
| **Fix Inline** (selected) | Resolve every API change in the same upgrade task — no deferred stubs, clean result. |
| Defer Complex Changes | Apply simple replacements inline; stub complex changes and create resolution subtasks. |

## Modernization

### Nullable Reference Types
Target is net10.0, all projects are C#, and nullable is not yet enabled — however with 11 projects it is better deferred as a separate effort.

| Value | Description |
|-------|-------------|
| **Leave Disabled** (selected) | Do not enable nullable during this upgrade; tackle as a separate effort afterwards. |
| Enable Nullable Reference Types | Add `<Nullable>enable</Nullable>` to all project files; may require code updates to fix warnings. |
