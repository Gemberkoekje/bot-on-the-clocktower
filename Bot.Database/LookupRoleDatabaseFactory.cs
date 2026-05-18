using Bot.Api.Database;
using Marten;

namespace Bot.Database
{
    public interface ILookupRoleDatabaseFactory
    {
        ILookupRoleDatabase CreateLookupRoleDatabase(IDocumentStore documentStore);
    }

    public class LookupRoleDatabaseFactory : ILookupRoleDatabaseFactory
    {
        public ILookupRoleDatabase CreateLookupRoleDatabase(IDocumentStore documentStore)
        {
            return new LookupRoleDatabase(documentStore);
        }
    }
}
