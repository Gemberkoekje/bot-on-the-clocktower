using Bot.Api;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway;
using Remora.Rest.Core;
using Remora.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bot.Remora
{
    public class RemoraClient : IBotClient
    {
        private readonly string m_token;
        private readonly Dictionary<ulong, IGuild> m_guilds = new();
        private readonly IComponentService m_componentService;
        private readonly DiscordGatewayClient? m_gatewayClient;
        private readonly SlashService? m_slashService;

        private CancellationTokenSource? m_gatewayStop;
        private Task? m_gatewayTask;

        public RemoraCommandRegistrationPlan CommandRegistrationPlan { get; }

        public event EventHandler<EventArgs>? Connected;
        public event EventHandler<MessageCreatedEventArgs>? MessageCreated;

        public bool IsConnected { get; private set; }

        public RemoraClient(
            IEnvironment environment,
            IComponentService? componentService = null,
            DiscordGatewayClient? gatewayClient = null,
            SlashService? slashService = null)
        {
            m_token = environment.GetEnvironmentVariable("DISCORD_TOKEN") ?? string.Empty;
            m_componentService = componentService ?? new NoOpComponentService();
            m_gatewayClient = gatewayClient;
            m_slashService = slashService;

            if (string.IsNullOrWhiteSpace(m_token))
            {
                throw new InvalidDiscordTokenException();
            }

            RemoraCommandRegistrationPlanner planner = new();
            try
            {
                CommandRegistrationPlan = planner.Build(environment);
            }
            catch (RemoraCommandRegistrationPlanner.InvalidDeployTypeException e)
            {
                throw new InvalidDeployTypeException(e.DeployType);
            }
        }

        public async Task ConnectAsync()
        {
            if (!IsConnected)
            {
                Console.WriteLine("RemoraClient: connecting and registering Discord commands.");
                await ApplyCommandRegistrationPlanAsync();
                if (m_gatewayClient is not null)
                {
                    m_gatewayStop = new CancellationTokenSource();
                    m_gatewayTask = Task.Run(async () =>
                    {
                        try
                        {
                            Console.WriteLine("RemoraClient: Discord gateway RunAsync invoking...");
                            Result runResult = await m_gatewayClient.RunAsync(m_gatewayStop.Token);
                            if (!runResult.IsSuccess)
                            {
                                Console.Error.WriteLine($"RemoraClient: Discord gateway RunAsync returned failure. Error={runResult.Error?.GetType().Name}, Message={runResult.Error?.Message}, Inner={runResult.Inner?.Error?.Message}");
                            }
                            else
                            {
                                Console.WriteLine("RemoraClient: Discord gateway RunAsync returned success (gateway stopped cleanly).");
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            Console.WriteLine("RemoraClient: Discord gateway cancelled.");
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine($"RemoraClient: Discord gateway task faulted. {e}");
                        }
                    });

                    Console.WriteLine("RemoraClient: Discord gateway run loop started.");
                }
                else
                {
                    Console.WriteLine("RemoraClient: Discord gateway client is not available; bot will remain offline.");
                }

                IsConnected = true;
                Connected?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task DisconnectAsync()
        {
            if (!IsConnected)
            {
                return;
            }

            IsConnected = false;

            if (m_gatewayStop is not null)
            {
                m_gatewayStop.Cancel();
            }

            if (m_gatewayTask is not null)
            {
                try
                {
                    await m_gatewayTask;
                }
                catch (OperationCanceledException)
                {
                }
            }

            m_gatewayStop?.Dispose();
            m_gatewayStop = null;
            m_gatewayTask = null;
        }

        public Task<IGuild?> GetGuildAsync(ulong guildId)
        {
            return Task.FromResult(m_guilds.GetValueOrDefault(guildId));
        }

        public void RegisterGuild(IGuild guild)
        {
            m_guilds[guild.Id] = guild;
            // Log bot role permissions for debugging
            Task.Run(async () => await LogBotRolePermissionsAsync(guild));
        }

        private async Task LogBotRolePermissionsAsync(IGuild guild)
        {
            try
            {
                IRole? botRole = guild.BotRole;
                if (botRole is null)
                {
                    Console.WriteLine($"RemoraClient: Bot role not found for guild {guild.Id} ({guild.Name})");
                    return;
                }

                Console.WriteLine($"RemoraClient: Guild registered: {guild.Name} (Id={guild.Id}), BotRole: {botRole.Name} (Id={botRole.Id})");

                // Log category permissions
                foreach (var category in guild.ChannelCategories)
                {
                    Console.WriteLine($"RemoraClient: Category '{category.Name}' (Id={category.Id})");
                }

                // Log channel permissions
                foreach (var channel in guild.Channels)
                {
                    if (!channel.IsText && !channel.IsVoice)
                        continue;

                    string channelType = channel.IsText ? "TEXT" : channel.IsVoice ? "VOICE" : "UNKNOWN";
                    Console.WriteLine($"RemoraClient: Channel '{channel.Name}' (Id={channel.Id}, Type={channelType})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RemoraClient: Error logging guild permissions. Exception={ex.GetType().Name}, Message={ex.Message}");
            }
        }

        public Task<bool> DispatchComponentInteractionAsync(IGuild guild, IChannel channel, IMember member, string customId, IEnumerable<string> values)
        {
            RemoraInteractionContext context = new(guild, channel, member, customId, values.ToArray());
            return m_componentService.CallAsync(context);
        }

        public void RaiseMessageCreated(IChannel channel, string message, bool isBotMessage = false)
        {
            if (isBotMessage)
            {
                return;
            }

            MessageCreated?.Invoke(this, new MessageCreatedEventArgs(channel, message));
        }

        private async Task ApplyCommandRegistrationPlanAsync()
        {
            if (m_slashService is null)
            {
                Console.WriteLine("RemoraClient: SlashService unavailable; skipping slash command registration.");
                return;
            }

            if (CommandRegistrationPlan.DeployType.Equals("dev", StringComparison.Ordinal)
                && CommandRegistrationPlan.DevGuildIds.Count > 0)
            {
                foreach (ulong guildId in CommandRegistrationPlan.DevGuildIds)
                {
                    Console.WriteLine($"RemoraClient: Registering Remora command tree to dev guild {guildId} via SlashService.");
                    Result slashResult = await m_slashService.UpdateSlashCommandsAsync(new Snowflake(guildId));
                    if (!slashResult.IsSuccess)
                    {
                        Console.Error.WriteLine($"RemoraClient: SlashService guild registration failed for guild {guildId}. Error={slashResult.Error?.GetType().Name}, Message={slashResult.Error?.Message}");
                    }
                    else
                    {
                        Console.WriteLine($"RemoraClient: SlashService guild registration succeeded for guild {guildId}.");
                    }
                }

                return;
            }

            Console.WriteLine("RemoraClient: Registering Remora command tree globally via SlashService.");
            Result globalResult = await m_slashService.UpdateSlashCommandsAsync();
            if (!globalResult.IsSuccess)
            {
                Console.Error.WriteLine($"RemoraClient: SlashService global registration failed. Error={globalResult.Error?.GetType().Name}, Message={globalResult.Error?.Message}");
            }
            else
            {
                Console.WriteLine("RemoraClient: SlashService global registration succeeded.");
            }
        }

        public class InvalidDiscordTokenException : Exception { }

        public class InvalidDeployTypeException : Exception
        {
            public string DeployType { get; }

            public InvalidDeployTypeException(string deployType)
                : base($"Bot must be configured with either 'dev' or 'prod' DEPLOY_TYPE. Actual value: '{deployType}'.")
            {
                DeployType = deployType;
            }
        }

        private sealed class NoOpComponentService : IComponentService
        {
            public void RegisterComponent(IBotComponent component, Func<IBotInteractionContext, Task> callback)
            {
            }

            public Task<bool> CallAsync(IBotInteractionContext context)
            {
                return Task.FromResult(false);
            }
        }
    }
}
