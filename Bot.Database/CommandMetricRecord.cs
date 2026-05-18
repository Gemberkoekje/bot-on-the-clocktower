using Bot.Api.Database;
using System;
using System.Collections.Generic;

namespace Bot.Database
{
    internal class CommandMetricRecord : ICommandMetricRecord
    {
        public DateTime Day { get; set; }

        public Dictionary<string, int> Commands { get; set; } = new Dictionary<string, int>();
    }
}
