using Bot.Api.Database;
using Marten;

namespace Bot.Database
{
    public interface IGameActivityDatabaseFactory
    {
        IGameActivityDatabase CreateGameActivityDatabase(IDocumentStore documentStore);
    }

    public class GameActivityDatabaseFactory : IGameActivityDatabaseFactory
    {
        public IGameActivityDatabase CreateGameActivityDatabase(IDocumentStore documentStore)
        {
            return new GameActivityDatabase(documentStore);
        }
    }
}
