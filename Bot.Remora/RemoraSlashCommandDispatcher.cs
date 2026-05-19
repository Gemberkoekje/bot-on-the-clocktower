using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace Bot.Remora
{
    internal sealed class RemoraSlashCommandDispatcher : IRemoraSlashCommandDispatcher
    {
        private readonly IDiscordRestInteractionAPI m_interactionApi;
        private readonly IDiscordRestGuildAPI? m_guildApi;
        private readonly RemoraSlashCommandRegistry m_registry;
        private IReadOnlyDictionary<string, IRemoraSlashCommand>? m_commands;

        public RemoraSlashCommandDispatcher(RemoraSlashCommandRegistry registry, IDiscordRestInteractionAPI interactionApi, IDiscordRestGuildAPI? guildApi = null)
        {
            m_registry = registry;
            m_interactionApi = interactionApi;
            m_guildApi = guildApi;
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

            IReadOnlyDictionary<string, object> arguments = BindArguments(command, slashData);
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

        private static IReadOnlyDictionary<string, object> BindArguments(IRemoraSlashCommand command, IApplicationCommandData slashData)
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
                if (!optionMap.TryGetValue(parameter.Name, out var option))
                {
                    if (parameter.IsRequired)
                    {
                        throw new ArgumentException($"Missing required option '{parameter.Name}' for command '{command.Name}'.");
                    }

                    continue;
                }

                if (!TryBindOption(parameter, option, slashData, out object value))
                {
                    throw new ArgumentException($"Option '{option.Name}' has an unsupported value for parameter type '{parameter.ParameterType}'.");
                }

                bound[parameter.Name] = value;
            }

            return bound;
        }

        private static bool TryBindOption(
            RemoraSlashCommandParameter parameter,
            IApplicationCommandInteractionDataOption option,
            IApplicationCommandData slashData,
            out object value)
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

                case RemoraSlashCommandParameterType.User:
                    return TryBindMemberOption(option, slashData, out value);

                case RemoraSlashCommandParameterType.Role:
                    return TryBindRoleOption(option, slashData, out value);

                case RemoraSlashCommandParameterType.Channel:
                    return TryBindChannelOption(option, slashData, out value);
            }

            return false;
        }

        private static bool TryBindMemberOption(IApplicationCommandInteractionDataOption option, IApplicationCommandData slashData, out object value)
        {
            value = string.Empty;
            if (!TryGetResolvedData(slashData, out IApplicationCommandInteractionDataResolved resolved))
            {
                return false;
            }

            if (!TryGetOptionSnowflake(option, out Snowflake memberId))
            {
                return false;
            }

            if (!resolved.Users.HasValue || !resolved.Users.Value.TryGetValue(memberId, out IUser? user) || user is null)
            {
                return false;
            }

            Optional<string?> nickname = default;
            Optional<IReadOnlyList<Snowflake>> roleIds = default;
            if (resolved.Members.HasValue && resolved.Members.Value.TryGetValue(memberId, out IPartialGuildMember? member) && member is not null)
            {
                nickname = member.Nickname;
                roleIds = member.Roles;
            }

            IReadOnlyDictionary<Snowflake, IRole> roles = resolved.Roles.HasValue
                ? resolved.Roles.Value
                : new Dictionary<Snowflake, IRole>();

            value = new ResolvedMemberAdapter(user, nickname, roleIds, roles);
            return true;
        }

        private static bool TryBindRoleOption(IApplicationCommandInteractionDataOption option, IApplicationCommandData slashData, out object value)
        {
            value = string.Empty;
            if (!TryGetResolvedData(slashData, out IApplicationCommandInteractionDataResolved resolved))
            {
                return false;
            }

            if (!TryGetOptionSnowflake(option, out Snowflake roleId))
            {
                return false;
            }

            if (!resolved.Roles.HasValue || !resolved.Roles.Value.TryGetValue(roleId, out IRole? role) || role is null)
            {
                return false;
            }

            value = new ResolvedRoleAdapter(role);
            return true;
        }

        private static bool TryBindChannelOption(IApplicationCommandInteractionDataOption option, IApplicationCommandData slashData, out object value)
        {
            value = string.Empty;
            if (!TryGetResolvedData(slashData, out IApplicationCommandInteractionDataResolved resolved))
            {
                return false;
            }

            if (!TryGetOptionSnowflake(option, out Snowflake channelId))
            {
                return false;
            }

            if (!resolved.Channels.HasValue || !resolved.Channels.Value.TryGetValue(channelId, out IPartialChannel? channel) || channel is null)
            {
                return false;
            }

            if (channel.Type.HasValue && channel.Type.Value == ChannelType.GuildCategory)
            {
                value = new ResolvedChannelCategoryAdapter(channel);
                return true;
            }

            value = new ResolvedChannelAdapter(channel);
            return true;
        }

        private static bool TryGetResolvedData(IApplicationCommandData slashData, out IApplicationCommandInteractionDataResolved resolved)
        {
            resolved = default!;
            if (!slashData.Resolved.HasValue)
            {
                return false;
            }

            resolved = slashData.Resolved.Value;
            return true;
        }

        private static bool TryGetOptionSnowflake(IApplicationCommandInteractionDataOption option, out Snowflake snowflake)
        {
            snowflake = default;
            if (!option.Value.HasValue)
            {
                return false;
            }

            OneOf<string, long, bool, Snowflake, double> raw = option.Value.Value;
            if (!raw.TryPickT3(out Snowflake id, out _))
            {
                return false;
            }

            snowflake = id;
            return true;
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

            string guildName = $"guild-{guildId}";
            RemoraGuild guild = m_guildApi is not null && guildId != 0UL
                ? new RemoraGuild(guildId, guildName, m_guildApi)
                : new RemoraGuild(guildId, guildName);
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
