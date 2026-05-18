using Bot.Api.Database;
using Marten;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Database
{
    public class LookupRoleDatabase : ILookupRoleDatabase
    {
        private readonly IDocumentStore m_documentStore;

        public LookupRoleDatabase(IDocumentStore documentStore)
        {
            m_documentStore = documentStore;
        }

        private async Task<LookupRoleRecord?> GetRecordInternal(ulong guildId)
        {
            using var querySession = m_documentStore.QuerySession();
            return await querySession.Query<LookupRoleRecord>()
                .FirstOrDefaultAsync(x => x.GuildId == guildId);
        }

        private async Task UpdateRecordInternal(LookupRoleRecord rec)
        {
            using var session = m_documentStore.LightweightSession();
            var existing = await session.Query<LookupRoleRecord>()
                .FirstOrDefaultAsync(x => x.GuildId == rec.GuildId);
            if (existing != null)
            {
                session.Delete(existing);
            }
            session.Store(rec);
            await session.SaveChangesAsync();
        }

        public async Task AddScriptUrlAsync(ulong guildId, string url)
        {
            var doc = await GetRecordInternal(guildId);

            if (doc == null)
            {
                doc = new LookupRoleRecord
                {
                    GuildId = guildId,
                    Urls = new List<string>()
                };
            }

            doc.Urls.Add(url);
            await UpdateRecordInternal(doc);
        }

        public async Task<IReadOnlyCollection<string>> GetScriptUrlsAsync(ulong guildId)
        {
            var doc = await GetRecordInternal(guildId);
            return doc != null ? doc.Urls : Array.Empty<string>();
        }

        public async Task RemoveScriptUrlAsync(ulong guildId, string url)
        {
            var doc = await GetRecordInternal(guildId);
            if (doc == null)
                return;
            doc.Urls.Remove(url);
            await UpdateRecordInternal(doc);
        }

        public class MissingLookupRoleDatabaseException : Exception { }
    }
}
