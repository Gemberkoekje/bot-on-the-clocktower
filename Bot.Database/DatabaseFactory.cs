using Bot.Api;
using Bot.Base;
using Marten;
using System;

namespace Bot.Database
{
	public class DatabaseFactory
	{
		private readonly IServiceProvider m_serviceProvider;
		private readonly IEnvironment m_environment;
		private readonly IMartenDocumentStoreFactory m_documentStoreFactory;
		private readonly ITownDatabaseFactory m_townLookupFactory;
		private readonly IGameActivityDatabaseFactory m_gameActivityDatabaseFactory;
		private readonly ILookupRoleDatabaseFactory m_lookupRoleDatabaseFactory;
		private readonly IAnnouncementDatabaseFactory m_announcementDatabaseFactory;
		private readonly IGameMetricDatabaseFactory m_gameMetricDatabaseFactory;
		private readonly ICommandMetricDatabaseFactory m_commandMetricDatabaseFactory;

		public const string PostgresConnectionStringConfigKey = "ConnectionStrings:Postgres";
		public const string PostgresConnectionStringEnvironmentVar = "ConnectionStrings__Postgres";
		public const string PostgresConnectionStringLegacyEnvironmentVar = "POSTGRES_CONNECT";

		public DatabaseFactory(IServiceProvider serviceProvider)
		{
			m_serviceProvider = serviceProvider;
			serviceProvider.Inject(out m_environment);
			serviceProvider.Inject(out m_documentStoreFactory);
			serviceProvider.Inject(out m_townLookupFactory);
			serviceProvider.Inject(out m_gameActivityDatabaseFactory);
			serviceProvider.Inject(out m_lookupRoleDatabaseFactory);
			serviceProvider.Inject(out m_announcementDatabaseFactory);
			serviceProvider.Inject(out m_gameMetricDatabaseFactory);
			serviceProvider.Inject(out m_commandMetricDatabaseFactory);
		}

		public IServiceProvider Connect()
		{
			var documentStore = ConnectToDocumentStore();
			return CreateDatabaseServices(documentStore);
		}

		public IDocumentStore ConnectToDocumentStore()
		{
			var connectionString = m_environment.GetEnvironmentVariable(PostgresConnectionStringConfigKey)
				?? m_environment.GetEnvironmentVariable(PostgresConnectionStringEnvironmentVar)
				?? m_environment.GetEnvironmentVariable(PostgresConnectionStringLegacyEnvironmentVar);

			if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidPostgresConnectStringException();

			IDocumentStore documentStore = m_documentStoreFactory.CreateDocumentStore(connectionString);
			if (documentStore == null) throw new DocumentStoreNotCreatedException();

			return documentStore;
		}

		public IServiceProvider CreateDatabaseServices(IDocumentStore documentStore)
		{
			var childSp = new ServiceProvider(m_serviceProvider);
			childSp.AddService(m_townLookupFactory.CreateTownLookup(documentStore));
			childSp.AddService(m_gameActivityDatabaseFactory.CreateGameActivityDatabase(documentStore));
			childSp.AddService(m_lookupRoleDatabaseFactory.CreateLookupRoleDatabase(documentStore));
			childSp.AddService(m_announcementDatabaseFactory.CreateAnnouncementDatabase(documentStore));
			childSp.AddService(m_gameMetricDatabaseFactory.CreateGameMetricDatabase(documentStore));
			childSp.AddService(m_commandMetricDatabaseFactory.CreateCommandMetricDatabase(documentStore));
			return childSp;
		}

		public class InvalidPostgresConnectStringException : Exception { }
		public class DocumentStoreNotCreatedException : Exception { }
	}
}
