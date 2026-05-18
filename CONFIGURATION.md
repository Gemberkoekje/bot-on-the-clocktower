# Configuration

Bot on the Clocktower uses the standard .NET configuration stack. Settings are read, in order of increasing priority, from:

1. `Bot.Main/appsettings.json` — committed defaults (no secrets).
2. `Bot.Main/appsettings.{Environment}.json` — e.g. `appsettings.Development.json`.
3. `Bot.Main/usersettings.json` — your local secrets (git-ignored).
4. Visual Studio user secrets (via `dotnet user-secrets`).
5. Environment variables (use `__` for nesting, e.g. `Discord__Token`).
6. Legacy `.env` file at the solution root (loaded for backward compatibility only).

`DOTNET_ENVIRONMENT` (or `ASPNETCORE_ENVIRONMENT`) selects the environment-specific JSON file. Visual Studio sets this to `Development` automatically when debugging.

## Required values

- `Discord:Token` — Discord bot token.
- `Discord:DevGuildIds` — array of guild IDs to register slash commands to in dev mode.
- `ConnectionStrings:Postgres` — PostgreSQL connection string used by Marten.

## Optional values

- `Deployment:Type` — `"dev"` or `"prod"` (default `prod`).
  - `dev`: slash commands are registered per guild from `Discord:DevGuildIds` (instant), debug-level logging, console sink enabled if `Logging:Console:Enabled` is `true`.
  - `prod`: slash commands are registered globally (Discord may take up to an hour to propagate), information-level logging.
- `Logging:LogLevel:Default` — `Debug` | `Information` | `Warning` | `Error`.
- `Logging:Console:Enabled` — `true` to enable console logging (forced on in dev mode).
- `Logging:File:Path` — log file path (default `logs/botc.log`).
- `Logging:File:RollingInterval` — `Day` | `Hour` | `Month` | `Year`.
- `RESTRICT_ANNOUNCE` (environment variable) — when `true`, version announcements are only sent to guilds on the internal allow-list.

## Local dev setup (`usersettings.json`)

1. In `Bot.Main`, copy `usersettings.json.example` to `usersettings.json`.
2. Fill in values:

```json
{
  "$schema": "./usersettings.schema.json",
  "Discord": {
	"Token": "YOUR_DISCORD_BOT_TOKEN",
	"DevGuildIds": [123456789012345678]
  },
  "ConnectionStrings": {
	"Postgres": "Host=localhost;Port=5432;Database=botc;Username=postgres;Password=postgres"
  }
}
```

`usersettings.json` is ignored by git.

## Visual Studio user-secrets alternative

From `Bot.Main`:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Discord:Token" "YOUR_DISCORD_BOT_TOKEN"
dotnet user-secrets set "Discord:DevGuildIds:0" "123456789012345678"
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=botc;Username=postgres;Password=postgres"
```

## How to get Discord values

### Discord token

1. Go to https://discord.com/developers/applications
2. Create/select your application.
3. Open **Bot** tab.
4. Create bot if needed, then **Reset Token** / **Copy**.

### Dev guild ID

1. In Discord, enable **Developer Mode** (User Settings → Advanced).
2. Right-click your server → **Copy Server ID**.

## Bot scopes and permissions

When inviting your bot, select scopes:
- `bot`
- `applications.commands`

Permissions needed:
- Manage Channels
- Manage Roles
- Manage Nicknames
- Move Members
- View Channels
- Send Messages
- Manage Messages

## Notes about Postgres

Postgres is implemented via Marten in `Bot.Database.MartenDocumentStoreFactory`.
The app reads `ConnectionStrings:Postgres` from `appsettings.json` / `usersettings.json` / user secrets / environment variables.
The legacy environment variables `POSTGRES_CONNECT` and `ConnectionStrings__Postgres` are still accepted as a fallback.

Marten will create the schema and indexes automatically the first time the bot runs against an empty database. No manual migration is required for a fresh install.
