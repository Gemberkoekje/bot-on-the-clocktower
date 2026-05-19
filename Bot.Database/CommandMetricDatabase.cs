using Bot.Api.Database;
using Marten;
using System;
using System.Threading.Tasks;

namespace Bot.Database
{
    internal class CommandMetricDatabase : ICommandMetricDatabase
    {
        private readonly IDocumentStore m_documentStore;

        public CommandMetricDatabase(IDocumentStore documentStore)
        {
            m_documentStore = documentStore;
        }

        private static DateTime NormalizeDay(DateTime timestamp)
        {
            return DateTime.SpecifyKind(timestamp.Date, DateTimeKind.Unspecified);
        }

        private async Task<CommandMetricRecord?> GetExisting(DateTime timestamp)
        {
            DateTime normalizedDay = NormalizeDay(timestamp);

            using var querySession = m_documentStore.QuerySession();
            return await querySession.Query<CommandMetricRecord>()
                .FirstOrDefaultAsync(x => x.Day == normalizedDay);
        }

        private async Task<CommandMetricRecord> GetExistingOrNew(DateTime timestamp)
        {
            var rec = await GetExisting(timestamp);

            if (rec != null)
            {
                return rec;
            }

            return new CommandMetricRecord()
            {
                Day = NormalizeDay(timestamp),
            };
        }

        public async Task RecordCommand(string command, DateTime timestamp)
        {
            var rec = await GetExistingOrNew(timestamp);

            if (!rec.Commands.ContainsKey(command))
            {
                rec.Commands.Add(command, 0);
            }

            rec.Commands[command]++;

            using var session = m_documentStore.LightweightSession();
            DateTime normalizedDay = NormalizeDay(rec.Day);
            var existing = await session.Query<CommandMetricRecord>()
                .FirstOrDefaultAsync(x => x.Day == normalizedDay);
            if (existing != null)
            {
                session.Delete(existing);
            }
            session.Store(rec);
            await session.SaveChangesAsync();
        }
    }

    internal class MissingCommandMetricDatabaseException : Exception { }
}
