using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public RemoraCommandRegistrationPlan CommandRegistrationPlan { get; }

        public event EventHandler<EventArgs>? Connected;
        public event EventHandler<MessageCreatedEventArgs>? MessageCreated;

        public bool IsConnected { get; private set; }

        public RemoraClient(
            IEnvironment environment,
            IComponentService? componentService = null,
            IRemoraCommandRegistrar? commandRegistrar = null,
            RemoraSlashCommandRegistry? commandRegistry = null)
        {
            m_token = environment.GetEnvironmentVariable("DISCORD_TOKEN") ?? string.Empty;
            m_componentService = componentService ?? new NoOpComponentService();
            m_commandRegistrar = commandRegistrar ?? new NoOpRemoraCommandRegistrar();
            m_commandRegistry = commandRegistry;

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
                await ApplyCommandRegistrationPlanAsync();
                IsConnected = true;
                Connected?.Invoke(this, EventArgs.Empty);
            }
        }

        public Task DisconnectAsync()
        {
            IsConnected = false;
            return Task.CompletedTask;
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

            if (CommandRegistrationPlan.DeployType.Equals("dev", StringComparison.Ordinal))
            {
                await m_commandRegistrar.RegisterGuildCommandsAsync(CommandRegistrationPlan.DevGuildIds, commands);
                return;
            }

            if (CommandRegistrationPlan.ClearDevGuildCommands)
            {
                await m_commandRegistrar.ClearGuildCommandsAsync(CommandRegistrationPlan.DevGuildIds);
            }

            if (CommandRegistrationPlan.RegisterGlobalCommands)
            {
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
