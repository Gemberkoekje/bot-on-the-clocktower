using Bot.Api;
using System;
using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;

namespace Bot.Remora
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers Remora-specific implementations and the slash-command registry.
        /// Must be called after gameplay/lookup services so the slash command sources
        /// can resolve their dependencies.
        /// </summary>
        public static IServiceCollection AddRemoraServices(this IServiceCollection services)
        {
            services.AddSingleton<IBotSystem, RemoraSystem>();
            services.AddSingleton<IColorBuilder, RemoraColorBuilder>();
            services.AddSingleton<IRemoraInteractionResponder, RemoraInteractionResponder>();
            services.AddSingleton<IRemoraSlashCommandDispatcher, RemoraSlashCommandDispatcher>();
            services.AddSingleton<IRemoraComponentDispatcher, NoOpRemoraComponentDispatcher>();

            services.AddDiscordGateway(
                sp => sp.GetRequiredService<IEnvironment>().GetEnvironmentVariable("DISCORD_TOKEN"),
                _ => { });
            services.AddResponder<RemoraGatewayInteractionResponder>();

            services.AddSingleton<IRemoraCommandRegistrar, RemoraCommandRegistrar>();

            services.AddSingleton(sp =>
            {
                RemoraSlashCommandRegistry registry = new();
                registry.AddSource(() => new RemoraGameSlashCommands(sp.GetRequiredService<IBotGameplayInteractionHandler>()));
                registry.AddSource(() => new RemoraMessagingSlashCommands(sp.GetRequiredService<IBotMessaging>()));
                registry.AddSource(() => new RemoraLookupSlashCommands(sp.GetRequiredService<IBotLookupService>()));
                registry.AddSource(() => new RemoraMiscSlashCommands(sp.GetRequiredService<IAnnouncer>()));
                registry.AddSource(() => new RemoraSetupSlashCommands(sp.GetRequiredService<IBotSetup>()));
                return registry;
            });

            services.AddSingleton<IBotClient>(sp => new RemoraClient(
                sp.GetRequiredService<IEnvironment>(),
                sp.GetService(typeof(IComponentService)) as IComponentService,
                sp.GetService(typeof(IRemoraCommandRegistrar)) as IRemoraCommandRegistrar,
                sp.GetService(typeof(RemoraSlashCommandRegistry)) as RemoraSlashCommandRegistry,
                sp.GetService(typeof(DiscordGatewayClient)) as DiscordGatewayClient));

            return services;
        }
    }
}
