using Bot.Api.Database;
using Marten;

namespace Bot.Database
{
    public interface IGameMetricDatabaseFactory
    {
        IGameMetricDatabase CreateGameMetricDatabase(IDocumentStore documentStore);
    }

    public class GameMetricDatabaseFactory : IGameMetricDatabaseFactory
    {
        public IGameMetricDatabase CreateGameMetricDatabase(IDocumentStore documentStore)
        {
            return new GameMetricDatabase(documentStore);
        }
    }
}
