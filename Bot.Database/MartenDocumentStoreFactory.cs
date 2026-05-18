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
                    .Identity(x => x.Id)
                    .Index(x => x.GuildId)
                    .Index(x => x.ControlChannelId)
                    .Index(x => x.DayCategory);

                options.Schema.For<GameActivityRecord>()
                    .Index(x => x.GuildId)
                    .Index(x => x.ChannelId);

                options.Schema.For<LookupRoleRecord>()
                    .Identity(x => x.Id)
                    .Index(x => x.GuildId);

                options.Schema.For<AnnouncementRecord>()
                    .Identity(x => x.Id)
                    .Index(x => x.GuildId);

                options.Schema.For<GameMetricRecord>()
                    .Identity(x => x.TownHash)
                    .Index(x => x.Complete)
                    .Index(x => x.FirstActivity);

                options.Schema.For<CommandMetricRecord>()
                    .Identity(x => x.Id)
                    .Index(x => x.Day);
            });
        }
    }
}
