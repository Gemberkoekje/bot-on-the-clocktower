using Bot.Api;
using Bot.Api.Database;
using Marten;

namespace Bot.Database
{
    public interface ITownDatabaseFactory
    {
        ITownDatabase CreateTownLookup(IDocumentStore documentStore);
    }

    public class TownDatabaseFactory : ITownDatabaseFactory
    {
        public ITownDatabase CreateTownLookup(IDocumentStore documentStore) => new TownDatabase(documentStore);
    }
}
