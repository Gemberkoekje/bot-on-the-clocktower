using Bot.Database;
using Microsoft.Extensions.DependencyInjection;
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
            var services = new ServiceCollection();
            services.AddBotDatabaseServices();

            using var sp = services.BuildServiceProvider();
            var service = sp.GetService(serviceInterface);

            Assert.NotNull(service);
            Assert.IsType(serviceImpl, service);
        }
    }
}
