using Bot.Api.Database;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Bot.Database
{
    internal class CommandMetricRecord : ICommandMetricRecord
    {
        private DateTime m_day;

        public string Id { get; set; } = string.Empty;

        public DateTime Day
        {
            get => m_day;
            set
            {
                m_day = value.Date;
                Id = m_day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
        }

        public Dictionary<string, int> Commands { get; set; } = new Dictionary<string, int>();
    }
}
