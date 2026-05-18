using Bot.Api;
using Bot.Api.Database;
using Marten;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Database
{
    internal class GameActivityDatabase : IGameActivityDatabase
    {
        private readonly IDocumentStore m_documentStore;

        public GameActivityDatabase(IDocumentStore documentStore)
        {
            m_documentStore = documentStore;
        }

        public async Task<IGameActivityRecord> GetActivityRecord(TownKey townKey)
        {
            using var querySession = m_documentStore.QuerySession();
            return await querySession.Query<GameActivityRecord>()
                .FirstOrDefaultAsync(x => x.GuildId == townKey.GuildId && x.ChannelId == townKey.ControlChannelId);
        }

        public async Task<IEnumerable<IGameActivityRecord>> GetAllActivityRecords()
        {
            using var querySession = m_documentStore.QuerySession();
            var records = await querySession.Query<GameActivityRecord>().ToListAsync();
            return records;
        }

        public async Task ClearActivityAsync(TownKey townKey)
        {
            using var session = m_documentStore.LightweightSession();
            var existing = await session.Query<GameActivityRecord>()
                .FirstOrDefaultAsync(x => x.GuildId == townKey.GuildId && x.ChannelId == townKey.ControlChannelId);
            if (existing != null)
            {
                session.Delete(existing);
                await session.SaveChangesAsync();
            }
        }

        public async Task RecordActivityAsync(TownKey townKey, DateTime activityTime)
        {
            Serilog.Log.Verbose("Recording activity for {townKey} at {time}", townKey, activityTime);

            using var session = m_documentStore.LightweightSession();
            var existing = await session.Query<GameActivityRecord>()
                .FirstOrDefaultAsync(x => x.GuildId == townKey.GuildId && x.ChannelId == townKey.ControlChannelId);
            if (existing != null)
            {
                session.Delete(existing);
            }

            GameActivityRecord rec = new()
            {
                GuildId = townKey.GuildId,
                ChannelId = townKey.ControlChannelId,
                LastActivity = activityTime,
            };

            session.Store(rec);
            await session.SaveChangesAsync();
        }
    }

    public class MissingGameActivityDatabaseException : Exception { }
}
