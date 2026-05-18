# Progress Details — 02-cpm-setup

## What Was Done

- Created `Directory.Packages.props` at repo root with `ManagePackageVersionsCentrally=true`
- Declared `PackageVersion` entries for all 13 packages used by the 9 bot projects
- Stripped `Version` attributes from `PackageReference` entries in all 9 bot project files:
  `Bot.Core`, `Bot.Database`, `Bot.DSharp`, `Bot.Main`, `Test.Bot.Base`, `Test.Bot.Core`,
  `Test.Bot.Database`, `Test.Bot.DSharp`, `Test.Main`
- Discovered that the `DSharpPlus` git submodule projects (included in the solution) have their own
  `PackageReference` entries with `Version` attributes that conflicted with CPM
- Fixed by creating `DSharpPlus/Directory.Packages.props` with `ManagePackageVersionsCentrally=false`
  to opt the submodule out of CPM — no submodule source files modified
- Created `DSharpPlus/Directory.Build.props` to document the opt-out intent (MSBuild layer)
- `Newtonsoft.Json` version aligned to 13.0.2 (highest across solution; submodule used 13.0.2, bot projects used 13.0.1)
- Initialized the DSharpPlus git submodule (`git submodule update --init --recursive`) which was previously uninitialized

## Build / Test Results

- `dotnet restore`: succeeded (0 errors)
- `dotnet build`: succeeded — 0 errors, 31 warnings (all pre-existing: DSharpPlus CS8632 nullable annotations + SYSLIB0013 Uri.EscapeUriString in Bot.Core)

## Files Modified

- `Directory.Packages.props` — created
- `DSharpPlus/Directory.Packages.props` — created (CPM opt-out)
- `DSharpPlus/Directory.Build.props` — created (documents opt-out)
- `Bot.Core/Bot.Core.csproj` — stripped Version attributes
- `Bot.Database/Bot.Database.csproj` — stripped Version attributes
- `Bot.DSharp/Bot.DSharp.csproj` — stripped Version attributes
- `Bot.Main/Bot.Main.csproj` — stripped Version attributes
- `Test.Bot.Base/Test.Bot.Base.csproj` — stripped Version attributes
- `Test.Bot.Core/Test.Bot.Core.csproj` — stripped Version attributes
- `Test.Bot.Database/Test.Bot.Database.csproj` — stripped Version attributes
- `Test.Bot.DSharp/Test.Bot.DSharp.csproj` — stripped Version attributes
- `Test.Main/Test.Main.csproj` — stripped Version attributes
