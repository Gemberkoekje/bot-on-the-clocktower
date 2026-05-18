using Bot.Api.Database;
using System;

namespace Bot.Database
{
    internal class GameMetricRecord : IGameMetricRecord
    {
        public int TownHash { get; set; }

        public DateTime FirstActivity { get; set; }

        public DateTime LastActivity { get; set; }

        public bool Complete { get; set; }

        public int Days { get; set; }

        public int Nights { get; set; }

        public int Votes { get; set; }
    }
}
