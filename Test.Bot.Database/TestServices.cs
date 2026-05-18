using Bot.Database;
using System;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Database
{
    public class TestServices : TestBase
    {
        [Theory]
        [InlineData(typeof(IMartenDocumentStoreFactory), typeof(MartenDocumentStoreFactory))]
        [InlineData(typeof(ITownDatabaseFactory), typeof(TownDatabaseFactory))]
        [InlineData(typeof(IGameActivityDatabaseFactory), typeof(GameActivityDatabaseFactory))]
        [InlineData(typeof(ILookupRoleDatabaseFactory), typeof(LookupRoleDatabaseFactory))]
        public void RegisterServices_CreatesAllRequiredServices(Type serviceInterface, Type serviceImpl)
        {
            var newSp = ServiceFactory.RegisterServices(GetServiceProvider());
            var service = newSp.GetService(serviceInterface);

            Assert.NotNull(service);
            Assert.IsType(serviceImpl, service);
        }
    }
}
