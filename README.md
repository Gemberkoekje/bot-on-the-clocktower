# Bot on the Clocktower

A Discord bot to assist with running a game of Blood on the Clocktower on Discord

This bot handles setting up all the channels, roles, and permissions you need to play Blood on the Clocktower, as well as moving the players back and forth during the various phases of the game without needing to type in complex movement commands!

# Running Your Own Instance

If you'd like to run your own instance of this bot for development or private use, follow these setup instructions.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- A running [PostgreSQL](https://www.postgresql.org/) instance (used by [Marten](https://martendb.io/) for document storage)
- A Discord account
- A Discord server where you have administrator permissions (for testing)

> **Note:** As of the current major version, the bot uses **PostgreSQL via Marten** for persistence and **Remora.Discord** for its Discord integration. The legacy MongoDB and DSharpPlus code paths have been removed. See [ARCHITECTURE.md](ARCHITECTURE.md) for an overview of how the bot is structured.

## Discord Bot Setup

### 1. Create a Discord Application

1. Go to the [Discord Developer Portal](https://discord.com/developers/applications)
2. Click **"New Application"**
3. Give your application a name (e.g., "BotC Dev Bot")
4. Click **"Create"**

### 2. Create a Bot User

1. In your application, go to the **"Bot"** tab in the left sidebar
2. Click **"Add Bot"** and confirm
3. Under the bot's username, click **"Reset Token"** and then **"Copy"**
4. **Save this token** - you'll need it for configuration (see below)
5. Enable the following **Privileged Gateway Intents**:
   - ✅ Server Members Intent
   - ✅ Message Content Intent

### 3. Get Your Development Guild (Server) ID

1. Open Discord and enable Developer Mode:
   - User Settings → App Settings → Advanced → Developer Mode (toggle ON)
2. Right-click your test server in the server list
3. Click **"Copy Server ID"**
4. **Save this ID** - you'll need it for configuration

### 4. Invite Your Bot to Your Server

1. In the Discord Developer Portal, go to the **"OAuth2"** tab → **"URL Generator"**
2. Under **Scopes**, select:
   - ✅ `bot`
   - ✅ `applications.commands`
3. Under **Bot Permissions**, select:
   - ✅ Manage Channels
   - ✅ Manage Roles
   - ✅ Manage Nicknames
   - ✅ Move Members
   - ✅ View Channels
   - ✅ Send Messages
   - ✅ Manage Messages
   - ✅ Use Slash Commands
4. Copy the generated URL at the bottom
5. Open the URL in your browser and select your test server
6. Click **"Authorize"**

## Configuration

The bot uses a layered configuration system with the following priority (highest to lowest):

1. **Environment variables** (highest priority)
2. **`usersettings.json`** - Your personal secrets (not committed to git)
3. **`appsettings.Development.json`** - Development environment overrides
4. **`appsettings.json`** - Default configuration

### Quick Setup

1. Navigate to the `Bot.Main` directory
2. Copy the example file:
   ```bash
   cp usersettings.json.example usersettings.json
   ```
3. Edit `usersettings.json` with your secrets:
   ```json
   {
     "Discord": {
       "Token": "YOUR_BOT_TOKEN_FROM_STEP_2",
       "DevGuildIds": [
         123456789012345678
       ]
     },
     "ConnectionStrings": {
       "Postgres": "Host=localhost;Port=5432;Database=botc;Username=postgres;Password=postgres"
     }
   }
   ```
   - Replace `YOUR_BOT_TOKEN_FROM_STEP_2` with the token you copied from the Discord Developer Portal
   - Replace `123456789012345678` with your server ID from step 3
   - You can add multiple guild IDs as an array: `[111111, 222222, 333333]`
   - Set the `ConnectionStrings:Postgres` value to point at your local PostgreSQL instance

4. **Important**: The `usersettings.json` file is already in `.gitignore` and will not be committed to version control

### Configuration Reference

### Configuration Reference

For complete configuration documentation, see [CONFIGURATION.md](CONFIGURATION.md).

**Discord Settings:**
- `Discord:Token` - Your Discord bot token (required)
- `Discord:DevGuildIds` - Array of server IDs where slash commands will be registered in dev mode

**Database Settings:**
- `ConnectionStrings:Postgres` - PostgreSQL connection string used by Marten (required)
- Legacy `POSTGRES_CONNECT` environment variable is still accepted for backward compatibility

**Deployment Settings:**
- `Deployment:Type` - Either `"dev"` or `"prod"`
  - `dev`: Commands registered to specific guilds (instant), console logging enabled
  - `prod`: Commands registered globally (can take up to 1 hour to propagate), minimal logging

**Logging Settings:**
- `Logging:LogLevel:Default` - Minimum log level: `Debug`, `Information`, `Warning`, or `Error`
- `Logging:Console:Enabled` - Whether to log to console (automatically enabled in dev mode)
- `Logging:File:Path` - Log file path (default: `logs/botc.log`)
- `Logging:File:RollingInterval` - How often to create new log files: `Day`, `Hour`, `Month`, `Year`

### Environment-Specific Configuration

To run in development mode (recommended for testing):

**Option 1: Set environment variable**
```bash
# PowerShell
$env:DOTNET_ENVIRONMENT="Development"

# Command Prompt
set DOTNET_ENVIRONMENT=Development

# Linux/macOS
export DOTNET_ENVIRONMENT=Development
```

**Option 2: Use launch settings (Visual Studio)**
Visual Studio automatically sets `DOTNET_ENVIRONMENT=Development` when debugging.

### Legacy .env Support

The bot still supports `.env` files for backward compatibility. If you have an existing `.env` file in the solution root, it will be loaded, but `usersettings.json` is the recommended approach. See [CONFIGURATION.md](CONFIGURATION.md) for details and the full configuration reference.

## Building and Running

1. Open the solution in Visual Studio or your preferred IDE
2. Build the solution
3. Set `Bot.Main` as the startup project
4. Run the project (F5 in Visual Studio)

Or from the command line:
```bash
dotnet build
cd Bot.Main
dotnet run
```

## Verifying Setup

When the bot starts successfully, you should see:
- Log output indicating the bot has connected
- Your bot appearing online in your Discord server
- Slash commands registered to your server (in dev mode) or globally (in prod mode)

Try running `/createtown` in your server to verify everything works!

### If slash commands do not appear

- Confirm the bot was invited with both OAuth scopes: `bot` and `applications.commands`.
- Confirm `Use Slash Commands` is allowed in the server/channel for the members running commands.
- Check startup logs for command registration mode:
  - `dev`: commands are registered to configured `Discord:DevGuildIds` (should appear quickly).
  - `prod`: commands are registered globally (Discord propagation can take time).
- If running in `dev`, confirm logs show your expected guild ID (and name) being targeted.

# Invite the Public Bot

If you just want to use the bot without running your own instance, **\>\>** [invite the public bot](https://discord.com/api/oauth2/authorize?client_id=795055055509651456&permissions=419441680&scope=applications.commands%20bot) **\<\<**

For more information on the permissions it requests, see [Permission Details](#permission-details).

# Quick Start: `/createtown`

To quickly set up your town, simply send a command to the bot with the name of your town.
You can also specify whether you'd like to use the "Night Category" with Cottages via the `usenight` param - some players don't use it.

> `/createtown townname:"Ravenswood Bluff"`

This will create all the categories, channels, and roles needed by Ravenswood Bluff.

The bot supports more than 1 town per Discord server. With 2 differently-named towns, you can run 2 games at once on the same server.

## Explanation of the Setup

For more information on precisely what this command does (what categories, roles, and permissions are created), see the [Setup Details](SETUP.md) and [Command Details](COMMANDS.md) documentation.

# Gameplay

All players can gather in the Town Square channel while the Storyteller sets up the game on http://clocktower.online (or whatever mechanism your group uses).

When it's time for Night 1 to begin, the Storyteller should use the `/game` command to start a new game.

![image](https://user-images.githubusercontent.com/151635/162874601-a94936c7-de43-4c0b-ad08-6089f67f6dc3.png)

From here, they can hit the Night button to move into the Night phase of the game.

## Initial Evil Info
If desired, to distribute the Minion and Demon info (but not the Demon bluffs), the Storyteller can use the `/evil` command (see [Command Details](COMMANDS.md)) to quickly send messages to all the Evil players informing them of their teammates.

## Night Phase
During the Night, the Storyteller can visit Cottages as dictated by the night order (the players are alphabetized into the Cottages to make finding players easier). The permissions are set up to allow the Storyteller to screen-share with users in the Cottages (for instance, to show the Grimoire to the Spy or Widow).

Once the night phase is complete, the Storyteller presses the Day button (or uses the `/day` command) to bring the players back to the Town Square and begin the day.

## Day Phase
Players can switch to other Daytime channels to have semi-private conversations until the Storyteller is ready to open nominations. The Storyteller may use the Vote button (or `/vote` command) or set a Vote Timer using the dropdown (or `/voteTimer` command) to drag all the players back to the Town Square.

This cycle of night & day continues until there's a winner!

If you'd like to start a new game with a new Storyteller, the new Storyteller can run `/game`, `/night` or any other command when ready to take over Storytelling duties.

Once you're all done playing, a Storyteller can optionally push the End Game button (or run `/endgame`) to remove roles and nicknames, generally cleaning up the town (though the bot will run this automatically after a few hours).

If you have more than one Storyteller, check out the `/storytellers` command in the [Command Details](COMMANDS.md).

# A Word About Rate Limits

Discord sometimes limits how many commands a bot can execute in a given timeframe (for good reason).

This is most noticeable when you run `/night`, `/day`, or `/vote` in larger groups - frequently only some (usually about 10) of the players will initially be moved, and there will be a delay of several seconds before the rest are moved.

This is normal behavior and not something to worry about! Just be patient and everyone will move eventually. Make sure you wait for everyone to wake up from their cottages in the morning!

The bot randomizes the order people are moved in, so it won't end up leaving the whole evil team with 10 extra seconds to plot by coincidence.

If you run into much longer delays, failures to move, or other errors, do let us know, however.

# Command Details

For full details of all the supported commands, see the [Command Details](COMMANDS.md) documentation.

# Architecture

For a description of how the bot is implemented internally — including the project layout, the Discord and database integration layers, and the gameplay flow — see [ARCHITECTURE.md](ARCHITECTURE.md).

# Roadmap and Known Issues

For a list of currently missing features, ideas for improvement, and known concerns about running Blood on the Clocktower on Discord, see [IMPROVEMENTS.md](IMPROVEMENTS.md).

# Permission Details

The bot requests the following permissions:

| Permission | Why? |
| ---------- | ---- |
| Manage Channels | To create/destroy channels and categories with `/createtown` and `/destroytown` commands |
| Manage Roles | To grant/remove Storyteller and Villager roles<br/>To create/destroy roles with `/createtown` and `/destroytown` commands |
| Manage Nicknames | To add/remove **(ST)** for the Storyteller's nickname |
| Move Members | To move players to nighttime rooms or back to the Town Square |
| Manage Messages | To delete `/evil` command messages so players can't see who's evil |
| View Channels | Required for many operations |
| Send Messages | Required for many operations |
| Use Slash Commands | Required for users to invoke slash commands with the bot |

# Support

I can be contacted on Discord at lilserf#8712 with any issues or questions.
In general, please file a Github issue with lots of details if you run into problems.
Of course, we're just doing this in our spare time and the bot features have primarily been driven by what our local play group needs, so please be patient.
