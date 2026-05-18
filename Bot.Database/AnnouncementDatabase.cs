using Bot.Api.Database;
using Marten;
using System;
using System.Threading.Tasks;

namespace Bot.Database
{
    internal class AnnouncementDatabase : IAnnouncementDatabase
    {
        private readonly IDocumentStore m_documentStore;

        public AnnouncementDatabase(IDocumentStore documentStore)
        {
            m_documentStore = documentStore;
        }

        private async Task<AnnouncementRecord> GetRecord(ulong guildId)
        {
            using var querySession = m_documentStore.QuerySession();
            return await querySession.Query<AnnouncementRecord>()
                .FirstOrDefaultAsync(x => x.GuildId == guildId);
        }

        public async Task<bool> HasSeenVersion(ulong guildId, Version version)
        {
            var record = await GetRecord(guildId);

            if (record == null)
                return false;

            return record.Version >= version;
        }

        public async Task RecordGuildHasSeenVersion(ulong guildId, Version version, bool force = false)
        {
            var record = await GetRecord(guildId);

            if (record == null || version > record.Version || force)
            {
                AnnouncementRecord rec = new()
                {
                    Id = guildId.ToString(),
                    GuildId = guildId,
                    Version = version,
                };

                using var session = m_documentStore.LightweightSession();
                if (record != null)
                {
                    session.Delete(record);
                }
                session.Store(rec);
                await session.SaveChangesAsync();
            }
        }
    }

    internal class MissingAnnouncementDatabaseException : Exception { }
}
