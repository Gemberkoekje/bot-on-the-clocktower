using Bot.Api.Database;
using System.Collections.Generic;

namespace Bot.Database
{
    public class LookupRoleRecord : ILookupRoleRecord
    {
        public string Id { get; set; } = string.Empty;

        public ulong GuildId { get; set; }

        public List<string> Urls { get; set; } = new();
    }
}
