using Bot.Api;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.Commands.Extensions;
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
            services.AddSingleton<ILiveRemoraInteractionContextFactory, LiveRemoraInteractionContextFactory>();

            services.AddDiscordGateway(
                sp => sp.GetRequiredService<IEnvironment>().GetEnvironmentVariable("DISCORD_TOKEN"),
                _ => { });

            services.Configure<DiscordGatewayClientOptions>(options =>
            {
                options.Intents = GatewayIntents.Guilds
                    | GatewayIntents.GuildMembers
                    | GatewayIntents.GuildVoiceStates
                    | GatewayIntents.GuildMessages
                    | GatewayIntents.MessageContents;
            });

            // Minimal proof-of-life: use Remora's canonical command pipeline for /ping.
            // AddDiscordCommands(enableSlash: true) registers Remora's own InteractionResponder,
            // SlashService, and command tree. While we validate the pipeline end-to-end, we
            // intentionally do NOT register our custom RemoraGatewayInteractionResponder so it
            // does not race Remora's own dispatch.
            services.AddDiscordCommands(true)
                .AddCommandTree()
                    .WithCommandGroup<PingCommand>()
                    .WithCommandGroup<CreateTownCommand>()
                    .WithCommandGroup<GameCommands>()
                    .WithCommandGroup<MessagingCommands>()
                    .WithCommandGroup<LookupCommands>()
                    .WithCommandGroup<MiscCommands>()
                    .WithCommandGroup<SetupCommands>()
                .Finish();

            services.AddSingleton<IBotClient>(sp => new RemoraClient(
                sp.GetRequiredService<IEnvironment>(),
                sp.GetService(typeof(IComponentService)) as IComponentService,
                sp.GetService(typeof(DiscordGatewayClient)) as DiscordGatewayClient,
                sp.GetService(typeof(global::Remora.Discord.Commands.Services.SlashService)) as global::Remora.Discord.Commands.Services.SlashService));

            return services;
        }
    }
}
