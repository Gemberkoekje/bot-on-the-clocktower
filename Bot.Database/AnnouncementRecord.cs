using Bot.Api.Database;
using System;

namespace Bot.Database
{
    public class AnnouncementRecord : IAnnouncementRecord
    {
        public string Id { get; set; } = string.Empty;

        public ulong GuildId { get; set; }

        public Version Version { get; set; } = new Version();
    }
}
