# Progress Details — 03-upgrade-projects

## What Was Done

### TFM Bump
Updated `TargetFramework` from `net6.0` to `net10.0` in all 11 bot project files.

### Package Updates (Directory.Packages.props)
- `MongoDB.Driver`: 2.15.0 → 3.8.1 (security fix: GHSA-7j9m-j397-g4wx)
- `Newtonsoft.Json`: 13.0.2 → 13.0.4 (recommended upgrade)
- `Moq`: 4.17.2 → 4.20.72 (to pull in a safer transitive dependency tree)
- `SharpCompress`: 0.48.1 added (pins transitive dep pulled by MongoDB.Driver 3.x, fixes GHSA-6c8g-7p36-r338)

### Transitive Vulnerability Fixes
After upgrading MongoDB.Driver 2→3, two new transitive vulnerabilities appeared:
- `SharpCompress 0.30.1` (Moderate) via MongoDB.Driver — fixed by pinning `SharpCompress 0.48.1` as an explicit reference in `Bot.Database` and `Test.Bot.Database`
- `Newtonsoft.Json 9.0.1` (High) via Moq — fixed by pinning `Newtonsoft.Json 13.0.4` as explicit reference in `Test.Bot.Base` and `Test.Bot.Database`

### API Fixes (Uri.EscapeUriString → Uri.EscapeDataString)
Replaced deprecated `Uri.EscapeUriString` (removed in .NET 10) with `Uri.EscapeDataString` in:
- `Bot.Core/Lookup/OfficialWikiHelper.cs` — wiki URL path segment encoding
- `Bot.Core/Lookup/CustomScriptParser.cs` — almanac URL encoding
- `Bot.Core/Lookup/JsonParseUtil.cs` — image URL encoding

Note: Initially tried removing escaping entirely for full-URL fields, but tests `CharParse_UrlWithSpaces_EncodesProperly` and `CustomScript_JustMeta_ReturnsValidScriptNoCharacters` confirmed space-encoding behaviour is expected — reverted to `Uri.EscapeDataString`.

## Files Modified

**Project files (TFM bump):**
- Bot.Api/Bot.Api.csproj
- Bot.Base/Bot.Base.csproj
- Bot.Core/Bot.Core.csproj
- Bot.Database/Bot.Database.csproj
- Bot.DSharp/Bot.DSharp.csproj
- Bot.Main/Bot.Main.csproj
- Test.Bot.Base/Test.Bot.Base.csproj
- Test.Bot.Core/Test.Bot.Core.csproj
- Test.Bot.Database/Test.Bot.Database.csproj
- Test.Bot.DSharp/Test.Bot.DSharp.csproj
- Test.Main/Test.Main.csproj

**Package management:**
- Directory.Packages.props (MongoDB.Driver 3.8.1, Newtonsoft.Json 13.0.4, Moq 4.20.72, SharpCompress 0.48.1 added)
- Bot.Database/Bot.Database.csproj (SharpCompress pinned)
- Test.Bot.Base/Test.Bot.Base.csproj (Newtonsoft.Json + SharpCompress pinned)
- Test.Bot.Database/Test.Bot.Database.csproj (Newtonsoft.Json + SharpCompress pinned)

**Source fixes:**
- Bot.Core/Lookup/OfficialWikiHelper.cs
- Bot.Core/Lookup/CustomScriptParser.cs
- Bot.Core/Lookup/JsonParseUtil.cs

## Build / Test Results

- `dotnet build`: succeeded — 0 errors, 0 warnings
- `dotnet test`: 337 tests passed, 0 failed across all 5 test projects
  - Test.Bot.Base: 7/7
  - Test.Bot.Core: 290/290
  - Test.Bot.Database: 13/13
  - Test.Bot.DSharp: 24/24
  - Test.Main: 3/3
