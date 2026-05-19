using Bot.Api;
using Remora.Discord.Gateway;
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
        private readonly IRemoraCommandRegistrar m_commandRegistrar;
        private readonly RemoraSlashCommandRegistry? m_commandRegistry;
        private readonly DiscordGatewayClient? m_gatewayClient;

        private CancellationTokenSource? m_gatewayStop;
        private Task? m_gatewayTask;

        public RemoraCommandRegistrationPlan CommandRegistrationPlan { get; }

        public event EventHandler<EventArgs>? Connected;
        public event EventHandler<MessageCreatedEventArgs>? MessageCreated;

        public bool IsConnected { get; private set; }

        public RemoraClient(
            IEnvironment environment,
            IComponentService? componentService = null,
            IRemoraCommandRegistrar? commandRegistrar = null,
            RemoraSlashCommandRegistry? commandRegistry = null,
            DiscordGatewayClient? gatewayClient = null)
        {
            m_token = environment.GetEnvironmentVariable("DISCORD_TOKEN") ?? string.Empty;
            m_componentService = componentService ?? new NoOpComponentService();
            m_commandRegistrar = commandRegistrar ?? new NoOpRemoraCommandRegistrar();
            m_commandRegistry = commandRegistry;
            m_gatewayClient = gatewayClient;

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
                            await m_gatewayClient.RunAsync(m_gatewayStop.Token);
                        }
                        catch (OperationCanceledException)
                        {
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
            IReadOnlyCollection<IRemoraSlashCommand> commands = ResolveCommandsForRegistration();

            Console.WriteLine($"RemoraClient: Command registration plan: DeployType={CommandRegistrationPlan.DeployType}, CommandCount={commands.Count}.");

            if (CommandRegistrationPlan.DeployType.Equals("dev", StringComparison.Ordinal))
            {
                string guildIdList = string.Join(", ", CommandRegistrationPlan.DevGuildIds);
                Console.WriteLine($"RemoraClient: Dev mode detected. Registering slash commands to configured dev guild IDs: [{guildIdList}].");
                await m_commandRegistrar.RegisterGuildCommandsAsync(CommandRegistrationPlan.DevGuildIds, commands);
                return;
            }

            if (CommandRegistrationPlan.ClearDevGuildCommands)
            {
                Console.WriteLine($"RemoraClient: Prod mode startup cleanup enabled. Clearing commands from {CommandRegistrationPlan.DevGuildIds.Count} dev guild(s) before global registration.");
                await m_commandRegistrar.ClearGuildCommandsAsync(CommandRegistrationPlan.DevGuildIds);
            }

            if (CommandRegistrationPlan.RegisterGlobalCommands)
            {
                Console.WriteLine("RemoraClient: Registering global slash commands.");
                await m_commandRegistrar.RegisterGlobalCommandsAsync(commands);
            }
        }

        private IReadOnlyCollection<IRemoraSlashCommand> ResolveCommandsForRegistration()
        {
            if (m_commandRegistry is null)
            {
                return Array.Empty<IRemoraSlashCommand>();
            }

            return m_commandRegistry.ResolveCommands().ToArray();
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
