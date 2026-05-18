# Set up appsettings.json and Visual Studio User Secrets

## Goal
Move application configuration away from direct `.env`/environment-only reads toward the standard .NET configuration stack with `appsettings.json`, environment-specific JSON, Visual Studio user secrets, and environment variable overrides.

## Current State
- `Bot.Main\Program.cs` calls `DotEnv.Load(@"..\..\..\..\.env")` and then reads `DEPLOY_TYPE` with `Environment.GetEnvironmentVariable`.
- `ProgramEnvironment` implements `IEnvironment` by calling `Environment.GetEnvironmentVariable(key)`.
- Other components read settings through `IEnvironment`, including:
  - Discord token: `DISCORD_TOKEN`.
  - Deployment type: `DEPLOY_TYPE`.
  - Mongo database settings: `MONGO_CONNECT`, `MONGO_DB`.
- `Bot.Main` is a console app using `Microsoft.NET.Sdk`, not `Microsoft.NET.Sdk.Web`; user secrets support must be added explicitly.

## Target Design
- Add `appsettings.json` to `Bot.Main` for non-secret defaults.
- Add optional `appsettings.Development.json` support for local non-secret overrides.
- Add Visual Studio user secrets support for sensitive local development values.
- Add environment variables after JSON/user secrets so deployed values can override file-based settings.
- Keep `IEnvironment` temporarily as the compatibility layer, but back it with `Microsoft.Extensions.Configuration.IConfiguration`.
- Preserve existing flat keys initially (`DEPLOY_TYPE`, `DISCORD_TOKEN`, `MONGO_CONNECT`, `MONGO_DB`) to minimize downstream changes, then migrate to hierarchical keys in later database/Discord work.

## Microsoft Guidance Notes
- User secrets are stored outside the repository and are not checked into source control.
- For non-web console projects, add `Microsoft.Extensions.Configuration` and `Microsoft.Extensions.Configuration.UserSecrets` explicitly.
- Visual Studio can enable secrets through **Manage User Secrets**, which adds a `UserSecretsId` to the project file.
- Environment variables should remain available for deployed environments and can override JSON/user-secrets values.

## Implementation Steps
1. Add configuration package references to `Bot.Main`:
   - `Microsoft.Extensions.Configuration`
   - `Microsoft.Extensions.Configuration.Json`
   - `Microsoft.Extensions.Configuration.EnvironmentVariables`
   - `Microsoft.Extensions.Configuration.UserSecrets`
2. Add a `UserSecretsId` property to `Bot.Main\Bot.Main.csproj`.
3. Add `Bot.Main\appsettings.json` with safe, non-secret defaults, for example:
   - `DEPLOY_TYPE`: `prod` or `dev` as agreed.
   - Logging settings if desired.
   - Placeholder non-secret database/Discord section names only.
4. Configure `appsettings.json` to copy to the output directory.
5. Optionally add `Bot.Main\appsettings.Development.json` for local non-secret overrides and exclude it from source control if the team wants developer-specific values.
6. Replace `DotEnv.Load(...)` in `Program.Main` with a `ConfigurationBuilder` that loads providers in this order:
   - `appsettings.json`
   - `appsettings.{environment}.json`, optional
   - user secrets for `Program`
   - environment variables
   - command-line arguments if `Main` starts using `args`
7. Update `ProgramEnvironment` to accept an `IConfiguration` instance and resolve keys through configuration first.
8. Register the built configuration in `RegisterServices`, then register `IEnvironment` as `new ProgramEnvironment(configuration)`.
9. Update `Program.Main` to read `DEPLOY_TYPE` from the configuration abstraction rather than `Environment.GetEnvironmentVariable`.
10. Decide whether to keep `DotEnv` for backward compatibility:
	- Preferred: remove it after configuration parity is confirmed.
	- Transitional: load `.env` before configuration only for legacy local support, with environment variables still overriding JSON.
11. Add or update tests in `Test.Main` for `ProgramEnvironment` precedence and fallback behavior.
12. Run `Test.Main` and the full solution build.

## Suggested appsettings.json Shape
Use flat keys first for compatibility:

```json
{
  "DEPLOY_TYPE": "prod"
}
```

After Mongo/Postgres and DSharp/Remora migration work, consider moving to hierarchical settings:

```json
{
  "Deployment": {
	"Type": "prod"
  },
  "Discord": {
	"DevGuildIds": []
  },
  "ConnectionStrings": {
	"Postgres": ""
  }
}
```

Do not put `DISCORD_TOKEN` or real database passwords in committed JSON.

## Suggested User Secrets
Set local secrets through Visual Studio **Manage User Secrets** or `dotnet user-secrets` from `Bot.Main`:

```json
{
  "DISCORD_TOKEN": "local-development-token",
  "MONGO_CONNECT": "legacy-local-mongo-connection",
  "MONGO_DB": "legacy-local-mongo-database"
}
```

When the database migration is complete, replace Mongo secrets with the PostgreSQL connection string, for example:

```json
{
  "ConnectionStrings:Postgres": "Host=localhost;Port=5432;Database=botc;Username=botc;Password=local-password",
  "DISCORD_TOKEN": "local-development-token"
}
```

## Validation Checklist
- App starts without requiring a `.env` file.
- `DEPLOY_TYPE` can be read from `appsettings.json`.
- Visual Studio user secrets override JSON values for local development.
- Environment variables override JSON/user-secret values for deployment.
- Existing consumers of `IEnvironment.GetEnvironmentVariable(...)` continue to work.
- No secret values are committed.

## Risks and Open Questions
- The app currently uses a custom service provider rather than Microsoft dependency injection, so configuration must be registered manually.
- The existing `Main(string[] _)` discards command-line arguments; accepting `args` is needed if command-line configuration overrides are desired.
- The repository instruction file discourages adding new nullable annotations; keep new configuration APIs compatible with the existing nullable-enabled project without expanding nullable use unnecessarily.
