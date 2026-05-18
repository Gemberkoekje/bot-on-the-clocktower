using Bot.Api.Database;
using System;

namespace Bot.Database
{
    internal class GameActivityRecord : IGameActivityRecord
    {
        public ulong GuildId { get; set; }

        public ulong ChannelId { get; set; }

        public DateTime LastActivity { get; set; }
    }
}
