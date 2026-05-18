using Bot.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

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
            services.AddSingleton<ILogger>(Log.Logger);
            services.AddSingleton(configuration);
            services.AddSingleton<IEnvironment>(new ConfigurationEnvironment(configuration));
            services.AddSingleton<IDateTime, DateTimeStatic>();
            services.AddSingleton<ITask, TaskStatic>();
            return services;
        }
    }
}
