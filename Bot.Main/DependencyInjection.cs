using Bot.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace Bot.Main
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers the base, framework-level services needed by every bot project:
        /// logger, configuration, environment, and basic abstractions like ITask / IDateTime.
        /// </summary>
        public static IServiceCollection AddBotBaseServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<Serilog.ILogger>(Log.Logger);
            services.AddSingleton(configuration);
            services.AddSingleton<IEnvironment>(new ConfigurationEnvironment(configuration));
            services.AddSingleton<IDateTime, DateTimeStatic>();
            services.AddSingleton<ITask, TaskStatic>();

            // Bridge Microsoft.Extensions.Logging (used by Remora) into Serilog so gateway/responder
            // diagnostic logs are actually emitted. Without this, Remora logs go to a null logger and
            // failures like bad intents, identify rejection, or websocket errors are invisible.
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddProvider(new SerilogLoggerProvider(Log.Logger, dispose: false));
            });
            return services;
        }
    }
}
