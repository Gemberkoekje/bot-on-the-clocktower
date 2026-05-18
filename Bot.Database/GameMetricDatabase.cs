using Bot.Api;
using Bot.Api.Database;
using Marten;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Database
{
    internal class GameMetricDatabase : IGameMetricDatabase
    {
        private readonly IDocumentStore m_documentStore;

        public GameMetricDatabase(IDocumentStore documentStore)
        {
            m_documentStore = documentStore;
        }

        private static int TownHash(TownKey townKey)
        {
            return HashCode.Combine(townKey.GuildId, townKey.ControlChannelId);
        }

        private async Task<GameMetricRecord?> GetExisting(TownKey townKey)
        {
            using var querySession = m_documentStore.QuerySession();
            return await querySession.Query<GameMetricRecord>()
                .FirstOrDefaultAsync(x => x.TownHash == TownHash(townKey) && x.Complete == false);
        }

        private async Task<GameMetricRecord> GetExistingOrNew(TownKey townKey, DateTime timestamp)
        {
            var rec = await GetExisting(townKey);

            if (rec != null)
            {
                rec.LastActivity = timestamp;
                return rec;
            }

            return new GameMetricRecord()
            {
                TownHash = TownHash(townKey),
                FirstActivity = timestamp,
                LastActivity = timestamp,
            };
        }

        private async Task UpsertByTownHash(GameMetricRecord record)
        {
            using var session = m_documentStore.LightweightSession();
            var existing = await session.Query<GameMetricRecord>()
                .FirstOrDefaultAsync(x => x.TownHash == record.TownHash && x.Complete == record.Complete);
            if (existing != null)
            {
                session.Delete(existing);
            }
            session.Store(record);
            await session.SaveChangesAsync();
        }

        public async Task RecordGameAsync(TownKey townKey, DateTime timestamp)
        {
            var existing = await GetExisting(townKey);
            if (existing != null)
            {
                existing.Complete = true;
                using var session = m_documentStore.LightweightSession();
                session.Store(existing);
                await session.SaveChangesAsync();
            }

            var newRec = new GameMetricRecord()
            {
                TownHash = TownHash(townKey),
                FirstActivity = timestamp,
                LastActivity = timestamp,
            };

            using (var session = m_documentStore.LightweightSession())
            {
                session.Store(newRec);
                await session.SaveChangesAsync();
            }
        }

        public async Task RecordDayAsync(TownKey townKey, DateTime timestamp)
        {
            var record = await GetExistingOrNew(townKey, timestamp);
            record.Days++;
            await UpsertByTownHash(record);
        }

        public async Task RecordNightAsync(TownKey townKey, DateTime timestamp)
        {
            var record = await GetExistingOrNew(townKey, timestamp);
            record.Nights++;
            await UpsertByTownHash(record);
        }

        public async Task RecordVoteAsync(TownKey townKey, DateTime timestamp)
        {
            var record = await GetExistingOrNew(townKey, timestamp);
            record.Votes++;
            await UpsertByTownHash(record);
        }

        public async Task RecordEndGameAsync(TownKey townKey, DateTime timestamp)
        {
            var record = await GetExistingOrNew(townKey, timestamp);
            record.Complete = true;
            await UpsertByTownHash(record);
        }

        public async Task<DateTime?> GetMostRecentGameAsync(TownKey townKey)
        {
            using var querySession = m_documentStore.QuerySession();
            var mostRecent = await querySession.Query<GameMetricRecord>()
                .Where(x => x.TownHash == TownHash(townKey))
                .OrderByDescending(x => x.FirstActivity)
                .FirstOrDefaultAsync();
            return mostRecent?.FirstActivity;
        }
    }

    internal class MissingGameMetricDatabaseException : Exception { }
}
