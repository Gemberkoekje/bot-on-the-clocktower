# 03-upgrade-projects: Upgrade all projects to net10.0

Update the `TargetFramework` in all 11 project files from `net6.0` to `net10.0`, update package versions in `Directory.Packages.props` (including the `MongoDB.Driver` security fix from 2.15.0 → 3.8.1 and `Newtonsoft.Json` from 13.0.1 → 13.0.4), and resolve all source-incompatible API usages and behavioral changes flagged by the assessment.

The assessment flagged source-incompatible APIs (`Api.0002`) in `Bot.Api`, `Bot.Core`, `Bot.Database`, and `Test.Bot.Core` (the bulk of the 58 issues in `Test.Bot.Core`), and behavioral changes (`Api.0003`) in `Bot.Core`, `Bot.Main`, and `Test.Bot.Core`. All API issues are to be fixed inline — no stubs or deferral. The `MongoDB.Driver` upgrade from 2.x to 3.x is a major version bump and may require driver API adjustments in `Bot.Database` and its tests.

Projects to update (in dependency order for reference, though all are changed together):
- **Level 0**: `Bot.Api`, `Bot.Base`
- **Level 1**: `Bot.Core`, `Bot.Database`, `Bot.DSharp`, `Test.Bot.Base`
- **Level 2**: `Bot.Main`, `Test.Bot.Core`, `Test.Bot.Database`, `Test.Bot.DSharp`
- **Level 3**: `Test.Main`

**Done when**: All 11 projects target `net10.0`; `MongoDB.Driver` is at 3.8.1 and `Newtonsoft.Json` at 13.0.4; solution builds with 0 errors and 0 warnings; all source-incompatible API usages and behavioral change sites are resolved.
