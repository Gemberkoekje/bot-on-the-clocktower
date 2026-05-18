using Bot.Api;
using Bot.Api.Database;
using Bot.Database;
using Marten;
using Moq;
using System;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Database
{
	public class TestDatabase : TestBase
	{
		private const string MockConnectionString = "mock-conn-string";

		[Fact]
		public void ConnectToDocumentStore_NoConnString_ThrowsException()
		{
			RegisterMock(new Mock<IEnvironment>());
			DatabaseFactory db = new(GetServiceProvider());

			Assert.Throws<DatabaseFactory.InvalidPostgresConnectStringException>(db.ConnectToDocumentStore);
		}

		[Fact]
		public void ConnectToDocumentStore_NoStore_ThrowsException()
		{
			var mockEnv = RegisterMock(new Mock<IEnvironment>());
			mockEnv.Setup(e => e.GetEnvironmentVariable(It.Is<string>(s => s == DatabaseFactory.PostgresConnectionStringConfigKey))).Returns(MockConnectionString);

			RegisterMock(new Mock<IMartenDocumentStoreFactory>());
			DatabaseFactory db = new(GetServiceProvider());

			Assert.Throws<DatabaseFactory.DocumentStoreNotCreatedException>(db.ConnectToDocumentStore);
		}

		[Fact]
		public void ConnectToDocumentStore_StoreCreated_Works()
		{
			var mockEnv = RegisterMock(new Mock<IEnvironment>());
			mockEnv.Setup(e => e.GetEnvironmentVariable(It.Is<string>(s => s == DatabaseFactory.PostgresConnectionStringConfigKey))).Returns(MockConnectionString);

			var mockStore = new Mock<IDocumentStore>();
			var storeFactory = RegisterMock(new Mock<IMartenDocumentStoreFactory>());
			storeFactory.Setup(sf => sf.CreateDocumentStore(It.Is<string>(s => s == MockConnectionString))).Returns(mockStore.Object);
			DatabaseFactory db = new(GetServiceProvider());

			var result = db.ConnectToDocumentStore();

			Assert.Equal(mockStore.Object, result);
		}

		[Fact]
		public void CreateDbServices_CreatesTownLookup()
		{
			var mockStore = new Mock<IDocumentStore>(MockBehavior.Strict);
			var mockTownLookup = new Mock<ITownDatabase>(MockBehavior.Strict);
			var mockGameActivityDb = new Mock<IGameActivityDatabase>(MockBehavior.Strict);
			var mockLookupRoleDb = new Mock<ILookupRoleDatabase>(MockBehavior.Strict);
			var mockAnnouncementDb = new Mock<IAnnouncementDatabase>(MockBehavior.Strict);
			var mockGameMetricDb = new Mock<IGameMetricDatabase>(MockBehavior.Strict);
			var mockCommandMetricDb = new Mock<ICommandMetricDatabase>(MockBehavior.Strict);
			var mockLookupRoleDbFactory = RegisterMock(new Mock<ILookupRoleDatabaseFactory>(MockBehavior.Strict));
			var mockTownLookupFactory = RegisterMock(new Mock<ITownDatabaseFactory>(MockBehavior.Strict));
			var mockGameActivityDbFactory = RegisterMock(new Mock<IGameActivityDatabaseFactory>(MockBehavior.Strict));
			var mockAnnouncementDbFactory = RegisterMock(new Mock<IAnnouncementDatabaseFactory>(MockBehavior.Strict));
			var mockGameMetricDbFactory = RegisterMock(new Mock<IGameMetricDatabaseFactory>(MockBehavior.Strict));
			var mockCommandMetricDbFactory = RegisterMock(new Mock<ICommandMetricDatabaseFactory>(MockBehavior.Strict));

			mockGameActivityDbFactory.Setup(gadbf => gadbf.CreateGameActivityDatabase(It.Is<IDocumentStore>(ds => ds == mockStore.Object))).Returns(mockGameActivityDb.Object);
			mockLookupRoleDbFactory.Setup(lrdbf => lrdbf.CreateLookupRoleDatabase(It.Is<IDocumentStore>(ds => ds == mockStore.Object))).Returns(mockLookupRoleDb.Object);
			mockAnnouncementDbFactory.Setup(adbf => adbf.CreateAnnouncementDatabase(It.Is<IDocumentStore>(ds => ds == mockStore.Object))).Returns(mockAnnouncementDb.Object);
			mockGameMetricDbFactory.Setup(gmdbf => gmdbf.CreateGameMetricDatabase(It.Is<IDocumentStore>(ds => ds == mockStore.Object))).Returns(mockGameMetricDb.Object);
			mockCommandMetricDbFactory.Setup(gmdbf => gmdbf.CreateCommandMetricDatabase(It.Is<IDocumentStore>(ds => ds == mockStore.Object))).Returns(mockCommandMetricDb.Object);

			mockTownLookupFactory.Setup(tlf => tlf.CreateTownLookup(It.Is<IDocumentStore>(ds => ds == mockStore.Object))).Returns(mockTownLookup.Object);
			DatabaseFactory db = new(GetServiceProvider());

			var result = db.CreateDatabaseServices(mockStore.Object);

			mockTownLookupFactory.Verify(tlf => tlf.CreateTownLookup(It.Is<IDocumentStore>(ds => ds == mockStore.Object)), Times.Once);
			Assert.Equal(mockTownLookup.Object, result.GetService<ITownDatabase>());
		}
	}
}
