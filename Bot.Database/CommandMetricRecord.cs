using Bot.Api.Database;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Bot.Database
{
    public class CommandMetricRecord : ICommandMetricRecord
    {
        private DateTime m_day;

        public string Id { get; set; } = string.Empty;

        public DateTime Day
        {
            get => m_day;
            set
            {
                m_day = DateTime.SpecifyKind(value.Date, DateTimeKind.Unspecified);
                Id = m_day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
        }

        public Dictionary<string, int> Commands { get; set; } = new Dictionary<string, int>();
    }
}
