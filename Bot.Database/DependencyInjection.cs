using Bot.Api;
using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace Bot.Database
{
    public static class DependencyInjection
    {
        public const string PostgresConnectionStringConfigKey = "ConnectionStrings:Postgres";
        public const string PostgresConnectionStringEnvironmentVar = "ConnectionStrings__Postgres";
        public const string PostgresConnectionStringLegacyEnvironmentVar = "POSTGRES_CONNECT";

        /// <summary>
        /// Registers database factories and an <see cref="IDocumentStore"/> built from the configured
        /// connection string. Each concrete database (Town, GameActivity, etc.) is produced from
        /// the document store via its registered factory.
        /// </summary>
        public static IServiceCollection AddBotDatabaseServices(this IServiceCollection services)
        {
            // Factories — singletons with no configuration
            services.AddSingleton<ITownDatabaseFactory, TownDatabaseFactory>();
            services.AddSingleton<IGameActivityDatabaseFactory, GameActivityDatabaseFactory>();
            services.AddSingleton<IMartenDocumentStoreFactory, MartenDocumentStoreFactory>();
            services.AddSingleton<ILookupRoleDatabaseFactory, LookupRoleDatabaseFactory>();
            services.AddSingleton<IAnnouncementDatabaseFactory, AnnouncementDatabaseFactory>();
            services.AddSingleton<IGameMetricDatabaseFactory, GameMetricDatabaseFactory>();
            services.AddSingleton<ICommandMetricDatabaseFactory, CommandMetricDatabaseFactory>();

            // Document store — created once from configured connection string
            services.AddSingleton<IDocumentStore>(sp =>
            {
                var environment = sp.GetRequiredService<IEnvironment>();
                var connectionString = ResolveConnectionString(environment);
                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidPostgresConnectStringException();

                var documentStore = sp.GetRequiredService<IMartenDocumentStoreFactory>().CreateDocumentStore(connectionString);
                if (documentStore == null)
                    throw new DocumentStoreNotCreatedException();
                return documentStore;
            });

            // Concrete database services
            services.AddSingleton(sp => sp.GetRequiredService<ITownDatabaseFactory>().CreateTownLookup(sp.GetRequiredService<IDocumentStore>()));
            services.AddSingleton(sp => sp.GetRequiredService<IGameActivityDatabaseFactory>().CreateGameActivityDatabase(sp.GetRequiredService<IDocumentStore>()));
            services.AddSingleton(sp => sp.GetRequiredService<ILookupRoleDatabaseFactory>().CreateLookupRoleDatabase(sp.GetRequiredService<IDocumentStore>()));
            services.AddSingleton(sp => sp.GetRequiredService<IAnnouncementDatabaseFactory>().CreateAnnouncementDatabase(sp.GetRequiredService<IDocumentStore>()));
            services.AddSingleton(sp => sp.GetRequiredService<IGameMetricDatabaseFactory>().CreateGameMetricDatabase(sp.GetRequiredService<IDocumentStore>()));
            services.AddSingleton(sp => sp.GetRequiredService<ICommandMetricDatabaseFactory>().CreateCommandMetricDatabase(sp.GetRequiredService<IDocumentStore>()));

            return services;
        }

        private static string ResolveConnectionString(IEnvironment environment)
        {
            return environment.GetEnvironmentVariable(PostgresConnectionStringConfigKey)
                ?? environment.GetEnvironmentVariable(PostgresConnectionStringEnvironmentVar)
                ?? environment.GetEnvironmentVariable(PostgresConnectionStringLegacyEnvironmentVar);
        }

        public class InvalidPostgresConnectStringException : System.Exception { }
        public class DocumentStoreNotCreatedException : System.Exception { }
    }
}
