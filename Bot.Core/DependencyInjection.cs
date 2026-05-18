using Bot.Api;
using Bot.Core.Callbacks;
using Bot.Core.Interaction;
using Bot.Core.Lookup;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Threading;

namespace Bot.Core
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Core services with few dependencies. Safe to register early.
        /// </summary>
        public static IServiceCollection AddBotCoreServices(this IServiceCollection services, CancellationToken applicationCancelToken)
        {
            services.AddSingleton<IFinalShutdownService>(sp => new ShutdownService(sp.GetRequiredService<IDateTime>(), applicationCancelToken));
            services.AddSingleton<IShutdownPreventionService>(sp => (ShutdownService)sp.GetRequiredService<IFinalShutdownService>());
            services.AddSingleton<IProcessLoggerFactory>(sp => new ProcessLoggerFactory(sp.GetRequiredService<ILogger>()));
            services.AddSingleton<ITownInteractionErrorHandler, TownInteractionErrorHandler>();
            services.AddSingleton<IGuildInteractionErrorHandler, GuildInteractionErrorHandler>();
            services.AddSingleton<ICallbackSchedulerFactory, CallbackSchedulerFactory>();
            services.AddSingleton<IComponentService, ComponentService>();
            services.AddSingleton<IShuffleService, ShuffleService>();

            services.AddBotCoreLookupServices();
            return services;
        }

        /// <summary>
        /// Services that depend on an <see cref="IBotSystem"/> and <see cref="IBotClient"/> being registered.
        /// </summary>
        public static IServiceCollection AddBotGameplayServices(this IServiceCollection services)
        {
            services.AddSingleton<BotSystemRunner>();
            services.AddSingleton<IGuildInteractionQueue, GuildInteractionQueue>();
            services.AddSingleton<ITownInteractionQueue, TownInteractionQueue>();
            services.AddSingleton<IGuildInteractionWrapper, GuildInteractionWrapper>();
            services.AddSingleton<ITownInteractionWrapper, TownInteractionWrapper>();
            services.AddSingleton<ITownMaintenance, TownMaintenance>();
            services.AddSingleton<ITownCleanup, TownCleanup>();
            services.AddSingleton<ITownResolver, TownResolver>();
            services.AddSingleton<BotGameplay>();
            services.AddSingleton<IVoteHandler>(sp => sp.GetRequiredService<BotGameplay>());
            services.AddSingleton<BotVoteTimer.VoteTimerController>();
            services.AddSingleton<BotVoteTimer>();
            services.AddSingleton<IBotGameplayInteractionHandler, BotGameplayInteractionHandler>();
            services.AddSingleton<IBotMessaging, BotMessaging>();
            services.AddSingleton<IBotSetup, BotSetup>();
            services.AddSingleton<IVersionProvider, VersionProvider>();
            services.AddSingleton<IAnnouncer, Announcer>();
            services.AddSingleton<ILegacyCommandReminder, LegacyCommandReminder>();
            services.AddSingleton<IGhostTownCleanup, GhostTownCleanup>();

            services.AddBotLookupServices();
            return services;
        }
    }
}
