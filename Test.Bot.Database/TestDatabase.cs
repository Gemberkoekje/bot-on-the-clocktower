using Bot.Api;
using Bot.Api.Database;
using Bot.Database;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Database
{
    public class TestDatabase : TestBase
    {
        private const string MockConnectionString = "mock-conn-string";

        private static ServiceCollection CreateServices()
        {
            var services = new ServiceCollection();
            services.AddBotDatabaseServices();
            return services;
        }

        [Fact]
        public void ConnectToDocumentStore_NoConnString_ThrowsException()
        {
            var env = new Mock<IEnvironment>();
            env.Setup(e => e.GetEnvironmentVariable(It.IsAny<string>())).Returns((string)null!);

            var services = CreateServices();
            services.AddSingleton(env.Object);
            using var sp = services.BuildServiceProvider();

            Assert.Throws<DependencyInjection.InvalidPostgresConnectStringException>(() => sp.GetRequiredService<IDocumentStore>());
        }

        [Fact]
        public void ConnectToDocumentStore_NoStore_ThrowsException()
        {
            var env = new Mock<IEnvironment>();
            env.Setup(e => e.GetEnvironmentVariable(DependencyInjection.PostgresConnectionStringConfigKey)).Returns(MockConnectionString);

            var storeFactory = new Mock<IMartenDocumentStoreFactory>();

            var services = CreateServices();
            services.AddSingleton(env.Object);
            services.AddSingleton(storeFactory.Object);
            using var sp = services.BuildServiceProvider();

            Assert.Throws<DependencyInjection.DocumentStoreNotCreatedException>(() => sp.GetRequiredService<IDocumentStore>());
        }

        [Fact]
        public void ConnectToDocumentStore_StoreCreated_Works()
        {
            var env = new Mock<IEnvironment>();
            env.Setup(e => e.GetEnvironmentVariable(DependencyInjection.PostgresConnectionStringConfigKey)).Returns(MockConnectionString);

            var mockStore = new Mock<IDocumentStore>();
            var storeFactory = new Mock<IMartenDocumentStoreFactory>();
            storeFactory.Setup(sf => sf.CreateDocumentStore(MockConnectionString)).Returns(mockStore.Object);

            var services = CreateServices();
            services.AddSingleton(env.Object);
            services.AddSingleton(storeFactory.Object);
            using var sp = services.BuildServiceProvider();

            Assert.Equal(mockStore.Object, sp.GetRequiredService<IDocumentStore>());
        }

        [Fact]
        public void CreateDbServices_CreatesTownLookup()
        {
            var env = new Mock<IEnvironment>();
            env.Setup(e => e.GetEnvironmentVariable(DependencyInjection.PostgresConnectionStringConfigKey)).Returns(MockConnectionString);

            var mockStore = new Mock<IDocumentStore>(MockBehavior.Loose);
            var storeFactory = new Mock<IMartenDocumentStoreFactory>();
            storeFactory.Setup(sf => sf.CreateDocumentStore(MockConnectionString)).Returns(mockStore.Object);

            var mockTownLookup = new Mock<ITownDatabase>(MockBehavior.Strict).Object;
            var mockGameActivityDb = new Mock<IGameActivityDatabase>(MockBehavior.Strict).Object;
            var mockLookupRoleDb = new Mock<ILookupRoleDatabase>(MockBehavior.Strict).Object;
            var mockAnnouncementDb = new Mock<IAnnouncementDatabase>(MockBehavior.Strict).Object;
            var mockGameMetricDb = new Mock<IGameMetricDatabase>(MockBehavior.Strict).Object;
            var mockCommandMetricDb = new Mock<ICommandMetricDatabase>(MockBehavior.Strict).Object;

            var townLookupFactory = new Mock<ITownDatabaseFactory>(MockBehavior.Strict);
            townLookupFactory.Setup(f => f.CreateTownLookup(mockStore.Object)).Returns(mockTownLookup);

            var gameActivityDbFactory = new Mock<IGameActivityDatabaseFactory>(MockBehavior.Strict);
            gameActivityDbFactory.Setup(f => f.CreateGameActivityDatabase(mockStore.Object)).Returns(mockGameActivityDb);

            var lookupRoleDbFactory = new Mock<ILookupRoleDatabaseFactory>(MockBehavior.Strict);
            lookupRoleDbFactory.Setup(f => f.CreateLookupRoleDatabase(mockStore.Object)).Returns(mockLookupRoleDb);

            var announcementDbFactory = new Mock<IAnnouncementDatabaseFactory>(MockBehavior.Strict);
            announcementDbFactory.Setup(f => f.CreateAnnouncementDatabase(mockStore.Object)).Returns(mockAnnouncementDb);

            var gameMetricDbFactory = new Mock<IGameMetricDatabaseFactory>(MockBehavior.Strict);
            gameMetricDbFactory.Setup(f => f.CreateGameMetricDatabase(mockStore.Object)).Returns(mockGameMetricDb);

            var commandMetricDbFactory = new Mock<ICommandMetricDatabaseFactory>(MockBehavior.Strict);
            commandMetricDbFactory.Setup(f => f.CreateCommandMetricDatabase(mockStore.Object)).Returns(mockCommandMetricDb);

            var services = CreateServices();
            services.AddSingleton(env.Object);
            services.AddSingleton(storeFactory.Object);
            services.AddSingleton(townLookupFactory.Object);
            services.AddSingleton(gameActivityDbFactory.Object);
            services.AddSingleton(lookupRoleDbFactory.Object);
            services.AddSingleton(announcementDbFactory.Object);
            services.AddSingleton(gameMetricDbFactory.Object);
            services.AddSingleton(commandMetricDbFactory.Object);

            using var sp = services.BuildServiceProvider();

            Assert.Equal(mockTownLookup, sp.GetRequiredService<ITownDatabase>());
            townLookupFactory.Verify(f => f.CreateTownLookup(mockStore.Object), Times.Once);
        }
    }
}
