using Bot.Api.Database;
using System;

namespace Bot.Database
{
    public class GameActivityRecord : IGameActivityRecord
    {
        public string Id { get; set; } = string.Empty;

        public ulong GuildId { get; set; }

        public ulong ChannelId { get; set; }

        public DateTime LastActivity { get; set; }
    }
}
