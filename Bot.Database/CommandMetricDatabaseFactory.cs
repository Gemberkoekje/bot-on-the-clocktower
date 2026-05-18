using Bot.Api.Database;
using Marten;

namespace Bot.Database
{
    public interface ICommandMetricDatabaseFactory
    {
        ICommandMetricDatabase CreateCommandMetricDatabase(IDocumentStore documentStore);
    }

    public class CommandMetricDatabaseFactory : ICommandMetricDatabaseFactory
    {
        public ICommandMetricDatabase CreateCommandMetricDatabase(IDocumentStore documentStore)
        {
            return new CommandMetricDatabase(documentStore);
        }
    }
}
