using Bot.Api.Database;
using System.Collections.Generic;

namespace Bot.Database
{
    public class LookupRoleRecord : ILookupRoleRecord
    {
        public ulong GuildId { get; set; }

        public List<string> Urls { get; set; } = new();
    }
}
