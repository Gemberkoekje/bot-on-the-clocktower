using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;

namespace Bot.Remora
{
    internal sealed class RemoraSlashCommandDispatcher : IRemoraSlashCommandDispatcher
    {
        private const string UnsupportedEntityMessage = "This command uses user/role/channel options that are not available yet. Please try a command with only text/boolean/number inputs.";

        private readonly IDiscordRestInteractionAPI m_interactionApi;
        private readonly RemoraSlashCommandRegistry m_registry;
        private IReadOnlyDictionary<string, IRemoraSlashCommand>? m_commands;

        public RemoraSlashCommandDispatcher(RemoraSlashCommandRegistry registry, IDiscordRestInteractionAPI interactionApi)
        {
            m_registry = registry;
            m_interactionApi = interactionApi;
        }

        public async Task DispatchAsync(IInteraction interaction, CancellationToken cancellationToken = default)
        {
            if (!TryGetSlashData(interaction, out IApplicationCommandData slashData))
            {
                throw new InvalidOperationException("Interaction payload does not contain slash command data.");
            }

            string commandName = slashData.Name ?? string.Empty;
            if (!ResolveCommands().TryGetValue(commandName, out var command))
            {
                throw new InvalidOperationException($"Unknown slash command: {commandName}");
            }

            var stopwatch = Stopwatch.StartNew();
            LiveRemoraInteractionContext context = CreateContext(interaction, cancellationToken);
            await context.DeferInteractionResponse();

            if (HasEntityParameters(command))
            {
                Console.WriteLine($"RemoraSlashCommandDispatcher: command '{command.Name}' uses entity parameters; responding with phase-1 unsupported message.");
                await m_interactionApi.CreateFollowupMessageAsync(
                    interaction.ApplicationID,
                    interaction.Token,
                    new Optional<string>(UnsupportedEntityMessage),
                    default,
                    default,
                    default,
                    default,
                    default,
                    new Optional<MessageFlags>(MessageFlags.Ephemeral),
                    cancellationToken);
                return;
            }

            IReadOnlyDictionary<string, object> arguments = BindPrimitiveArguments(command, slashData);
            await command.InvokeAsync(context, arguments);

            stopwatch.Stop();
            Console.WriteLine(
                $"RemoraSlashCommandDispatcher: command='{command.Name}', interactionId={interaction.ID.Value}, guildId={context.Guild.Id}, channelId={context.Channel.Id}, memberId={context.Member.Id}, durationMs={stopwatch.ElapsedMilliseconds}, status=completed.");
        }

        private static bool TryGetSlashData(IInteraction interaction, out IApplicationCommandData slashData)
        {
            slashData = default!;
            if (!interaction.Data.HasValue)
            {
                return false;
            }

            OneOf<IApplicationCommandData, IMessageComponentData, IModalSubmitData> data = interaction.Data.Value;
            if (!data.TryPickT0(out slashData, out _))
            {
                return false;
            }

            return true;
        }

        private static bool HasEntityParameters(IRemoraSlashCommand command)
        {
            return command.Parameters.Any(parameter =>
                parameter.ParameterType == RemoraSlashCommandParameterType.User
                || parameter.ParameterType == RemoraSlashCommandParameterType.Role
                || parameter.ParameterType == RemoraSlashCommandParameterType.Channel);
        }

        private static IReadOnlyDictionary<string, object> BindPrimitiveArguments(IRemoraSlashCommand command, IApplicationCommandData slashData)
        {
            IReadOnlyList<IApplicationCommandInteractionDataOption> options = slashData.Options.HasValue
                ? slashData.Options.Value
                : Array.Empty<IApplicationCommandInteractionDataOption>();

            Dictionary<string, IApplicationCommandInteractionDataOption> optionMap = options
                .Where(option => !string.IsNullOrEmpty(option.Name))
                .GroupBy(option => option.Name, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToDictionary(option => option.Name, option => option, StringComparer.Ordinal);

            Dictionary<string, object> bound = new(StringComparer.Ordinal);
            foreach (RemoraSlashCommandParameter parameter in command.Parameters)
            {
                if (parameter.ParameterType != RemoraSlashCommandParameterType.String
                    && parameter.ParameterType != RemoraSlashCommandParameterType.Boolean
                    && parameter.ParameterType != RemoraSlashCommandParameterType.Integer)
                {
                    continue;
                }

                if (!optionMap.TryGetValue(parameter.Name, out var option))
                {
                    if (parameter.IsRequired)
                    {
                        throw new ArgumentException($"Missing required option '{parameter.Name}' for command '{command.Name}'.");
                    }

                    continue;
                }

                if (!TryBindPrimitiveOption(parameter, option, out object value))
                {
                    throw new ArgumentException($"Option '{option.Name}' has an unsupported value for parameter type '{parameter.ParameterType}'.");
                }

                bound[parameter.Name] = value;
            }

            return bound;
        }

        private static bool TryBindPrimitiveOption(RemoraSlashCommandParameter parameter, IApplicationCommandInteractionDataOption option, out object value)
        {
            value = string.Empty;
            if (!option.Value.HasValue)
            {
                return false;
            }

            OneOf<string, long, bool, Snowflake, double> raw = option.Value.Value;
            switch (parameter.ParameterType)
            {
                case RemoraSlashCommandParameterType.String:
                    if (raw.TryPickT0(out string text, out _))
                    {
                        value = text;
                        return true;
                    }
                    break;

                case RemoraSlashCommandParameterType.Boolean:
                    if (raw.TryPickT2(out bool boolean, out _))
                    {
                        value = boolean;
                        return true;
                    }
                    break;

                case RemoraSlashCommandParameterType.Integer:
                    if (raw.TryPickT1(out long integer, out _))
                    {
                        value = integer;
                        return true;
                    }
                    break;
            }

            return false;
        }

        private LiveRemoraInteractionContext CreateContext(IInteraction interaction, CancellationToken cancellationToken)
        {
            ulong guildId = interaction.GuildID.HasValue ? interaction.GuildID.Value.Value : 0UL;
            ulong channelId = interaction.Channel.HasValue && interaction.Channel.Value.ID.HasValue
                ? interaction.Channel.Value.ID.Value.Value
                : 0UL;
            string channelName = interaction.Channel.HasValue && interaction.Channel.Value.Name.HasValue
                ? interaction.Channel.Value.Name.Value ?? $"channel-{channelId}"
                : $"channel-{channelId}";
            ulong memberId = 0UL;
            string memberName = "unknown-member";
            if (interaction.Member.HasValue && interaction.Member.Value.User.HasValue)
            {
                memberId = interaction.Member.Value.User.Value.ID.Value;
                memberName = interaction.Member.Value.Nickname.HasValue
                    ? interaction.Member.Value.Nickname.Value ?? interaction.Member.Value.User.Value.Username ?? memberName
                    : interaction.Member.Value.User.Value.Username ?? memberName;
            }
            else if (interaction.User.HasValue)
            {
                memberId = interaction.User.Value.ID.Value;
                memberName = interaction.User.Value.Username ?? memberName;
            }

            RemoraGuild guild = new(guildId, $"guild-{guildId}");
            RemoraChannel channel = new(channelId, channelName);
            RemoraMember member = new(memberId, memberName);

            return new LiveRemoraInteractionContext(
                guild,
                channel,
                member,
                interactionApi: m_interactionApi,
                applicationId: interaction.ApplicationID,
                interactionId: interaction.ID,
                token: interaction.Token,
                cancellationToken: cancellationToken);
        }

        private IReadOnlyDictionary<string, IRemoraSlashCommand> ResolveCommands()
        {
            if (m_commands is not null)
            {
                return m_commands;
            }

            m_commands = m_registry.ResolveCommands()
                .GroupBy(command => command.Name, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToDictionary(command => command.Name, command => command, StringComparer.Ordinal);

            return m_commands;
        }
    }
}
