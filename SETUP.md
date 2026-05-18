# Setup Details

The expected setup for using the bot is to have a set of categories, channels, and roles representing a "Town".

For these setup examples, we will use a town named "Ravenswood Bluff", but you can use whatever town name you like.

## Roles

It is recommended (but optional) that your server has the following roles:
* A role for server members who like to be Storytellers. Example role name: **BotC Storyteller**
* A role for server members who play the game. Example role name: **BotC Player**
  * If your server is entirely based around playing Blood on the Clocktower, this is unnecessary.

You should grant this role to appropriate server members, as the bot will not grant these roles for you.

![image](https://user-images.githubusercontent.com/151635/162869896-faa53bac-4b13-4671-9c71-bbf42e56f563.png)

These roles combine with Town-specific roles to affect the visibility of the various channels involved in a town.

### A note on Server Administrators

Bot on the Clocktower works best by hiding nighttime channels from members. Unfortunately, server Administrators or Owners can always see all channels and cannot have their nicknames changed. For these reasons, if a server Administrator/Owner wants to play too, it is recommended that they create a separate non-Admininstrator/Owner Discord account for playing with the bot.

## Channels and Categories

The recommended layout is created automatically by `/createtown`. For full details of what categories, channels, and roles the bot expects, see the `/addtown` reference in [COMMANDS.md](COMMANDS.md).

## Related Documentation

- [README.md](README.md) — overview and quick start.
- [COMMANDS.md](COMMANDS.md) — full command reference.
- [CONFIGURATION.md](CONFIGURATION.md) — Discord/PostgreSQL configuration for self-hosting.
- [ARCHITECTURE.md](ARCHITECTURE.md) — internal implementation overview.
- [IMPROVEMENTS.md](IMPROVEMENTS.md) — known gaps and ideas.
