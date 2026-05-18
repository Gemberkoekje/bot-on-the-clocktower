using Marten;

namespace Bot.Database
{
    public interface IMartenDocumentStoreFactory
    {
        IDocumentStore CreateDocumentStore(string connectionString);
    }

    public class MartenDocumentStoreFactory : IMartenDocumentStoreFactory
    {
        public IDocumentStore CreateDocumentStore(string connectionString)
        {
            return DocumentStore.For(options =>
            {
                options.Connection(connectionString);

                options.Schema.For<TownRecord>()
                    .Index(x => x.GuildId)
                    .Index(x => x.ControlChannelId)
                    .Index(x => x.DayCategory);

                options.Schema.For<GameActivityRecord>()
                    .Index(x => x.GuildId)
                    .Index(x => x.ChannelId);

                options.Schema.For<LookupRoleRecord>()
                    .Index(x => x.GuildId);

                options.Schema.For<AnnouncementRecord>()
                    .Index(x => x.GuildId);

                options.Schema.For<GameMetricRecord>()
                    .Index(x => x.TownHash)
                    .Index(x => x.Complete)
                    .Index(x => x.FirstActivity);

                options.Schema.For<CommandMetricRecord>()
                    .Index(x => x.Day);
            });
        }
    }
}
