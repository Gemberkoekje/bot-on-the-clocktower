using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bot.Api;
using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace Bot.Remora
{
    internal sealed class RemoraComponentDispatcher : IRemoraComponentDispatcher
    {
        private readonly IComponentService m_componentService;
        private readonly IDiscordRestInteractionAPI m_interactionApi;

        public RemoraComponentDispatcher(IComponentService componentService, IDiscordRestInteractionAPI interactionApi)
        {
            m_componentService = componentService;
            m_interactionApi = interactionApi;
        }

        public async Task<bool> DispatchAsync(IInteraction interaction, CancellationToken cancellationToken = default)
        {
            if (!TryResolveInteractionData(interaction, out ResolvedInteractionData data))
            {
                throw new InvalidOperationException($"Interaction {interaction.ID.Value} of type {interaction.Type} does not contain component or modal data.");
            }

            LiveRemoraInteractionContext context = CreateContext(interaction, data.CustomId, data.Values, cancellationToken);
            bool handled = await m_componentService.CallAsync(context);

            Console.WriteLine(
                $"RemoraComponentDispatcher: interactionType={interaction.Type}, interactionId={interaction.ID.Value}, guildId={context.Guild.Id}, channelId={context.Channel.Id}, memberId={context.Member.Id}, customId='{data.CustomId}', valueCount={data.Values.Count}, modalSubmissionKeys='{string.Join(",", data.ModalSubmissionKeys)}', handled={handled}.");

            return handled;
        }

        private static bool TryResolveInteractionData(IInteraction interaction, out ResolvedInteractionData data)
        {
            data = new ResolvedInteractionData(string.Empty, Array.Empty<string>(), Array.Empty<string>());
            if (!interaction.Data.HasValue)
            {
                return false;
            }

            OneOf<IApplicationCommandData, IMessageComponentData, IModalSubmitData> payload = interaction.Data.Value;

            if (payload.TryPickT1(out IMessageComponentData componentData, out var rest))
            {
                data = new ResolvedInteractionData(
                    componentData.CustomID ?? string.Empty,
                    ExtractMessageComponentValues(componentData),
                    Array.Empty<string>());
                return true;
            }

            if (rest.TryPickT1(out IModalSubmitData modalData, out _))
            {
                IReadOnlyList<string> values = ExtractModalSubmitValues(modalData, out IReadOnlyList<string> modalSubmissionKeys);
                data = new ResolvedInteractionData(
                    modalData.CustomID ?? string.Empty,
                    values,
                    modalSubmissionKeys);
                return true;
            }

            return false;
        }

        private static IReadOnlyList<string> ExtractMessageComponentValues(IMessageComponentData componentData)
        {
            if (!componentData.Values.HasValue)
            {
                return Array.Empty<string>();
            }

            OneOf<IReadOnlyList<Snowflake>, IReadOnlyList<string>> rawValues = componentData.Values.Value;
            if (rawValues.TryPickT0(out IReadOnlyList<Snowflake> snowflakeValues, out IReadOnlyList<string> stringValues))
            {
                return snowflakeValues.Select(v => v.Value.ToString()).ToArray();
            }

            return stringValues.ToArray();
        }

        private static IReadOnlyList<string> ExtractModalSubmitValues(IModalSubmitData modalData, out IReadOnlyList<string> submissionKeys)
        {
            List<string> values = new();
            List<string> keys = new();

            foreach (IPartialMessageComponent component in modalData.Components)
            {
                CollectModalTextInputValues(component, keys, values);
            }

            submissionKeys = keys;
            return values;
        }

        private static void CollectModalTextInputValues(IPartialMessageComponent component, List<string> keys, List<string> values)
        {
            if (component is IPartialTextInputComponent textInput)
            {
                if (textInput.CustomID.HasValue && !string.IsNullOrWhiteSpace(textInput.CustomID.Value))
                {
                    keys.Add(textInput.CustomID.Value);
                }

                if (textInput.Value.HasValue)
                {
                    values.Add(textInput.Value.Value ?? string.Empty);
                }

                return;
            }

            if (component is IPartialActionRowComponent actionRow && actionRow.Components.HasValue)
            {
                foreach (IPartialMessageComponent nestedComponent in actionRow.Components.Value)
                {
                    CollectModalTextInputValues(nestedComponent, keys, values);
                }
            }
        }

        private LiveRemoraInteractionContext CreateContext(
            IInteraction interaction,
            string componentCustomId,
            IReadOnlyList<string> componentValues,
            CancellationToken cancellationToken)
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
                cancellationToken: cancellationToken,
                componentCustomId: componentCustomId,
                componentValues: componentValues);
        }

        private readonly struct ResolvedInteractionData
        {
            public ResolvedInteractionData(string customId, IReadOnlyList<string> values, IReadOnlyList<string> modalSubmissionKeys)
            {
                CustomId = customId;
                Values = values;
                ModalSubmissionKeys = modalSubmissionKeys;
            }

            public string CustomId { get; }

            public IReadOnlyList<string> Values { get; }

            public IReadOnlyList<string> ModalSubmissionKeys { get; }
        }
    }
}
