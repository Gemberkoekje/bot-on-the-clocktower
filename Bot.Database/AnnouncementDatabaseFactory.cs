using Bot.Api.Database;
using Marten;

namespace Bot.Database
{
    public interface IAnnouncementDatabaseFactory
    {
        IAnnouncementDatabase CreateAnnouncementDatabase(IDocumentStore documentStore);
    }

    public class AnnouncementDatabaseFactory : IAnnouncementDatabaseFactory
    {
        public IAnnouncementDatabase CreateAnnouncementDatabase(IDocumentStore documentStore)
        {
            return new AnnouncementDatabase(documentStore);
        }
    }
}
