using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bot.Api;
using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;

namespace Bot.Remora
{
    internal sealed class LiveRemoraInteractionContext : IBotInteractionContext
    {
        private readonly IDiscordRestInteractionAPI m_interactionApi;
        private readonly Snowflake m_applicationId;
        private readonly Snowflake m_interactionId;
        private readonly string m_token;
        private readonly CancellationToken m_cancellationToken;
        private readonly string? m_componentCustomId;
        private readonly IReadOnlyList<string> m_componentValues;

        public Bot.Api.IGuild Guild { get; }

        public Bot.Api.IChannel Channel { get; }

        public IMember Member { get; }

        public string? ComponentCustomId => m_componentCustomId;

        public IEnumerable<string> ComponentValues => m_componentValues;

        public bool IsDeferred { get; private set; }

        public LiveRemoraInteractionContext(
            Bot.Api.IGuild guild,
            Bot.Api.IChannel channel,
            IMember member,
            IDiscordRestInteractionAPI interactionApi,
            Snowflake applicationId,
            Snowflake interactionId,
            string token,
            CancellationToken cancellationToken,
            string? componentCustomId = null,
            IEnumerable<string>? componentValues = null)
        {
            Guild = guild;
            Channel = channel;
            Member = member;
            m_interactionApi = interactionApi;
            m_applicationId = applicationId;
            m_interactionId = interactionId;
            m_token = token;
            m_cancellationToken = cancellationToken;
            m_componentCustomId = componentCustomId;
            m_componentValues = componentValues?.ToArray() ?? Array.Empty<string>();
        }

        public async Task DeferInteractionResponse()
        {
            Console.WriteLine($"LiveRemoraInteractionContext: defer requested. InteractionId={m_interactionId.Value}, IsDeferred={IsDeferred}.");

            if (IsDeferred)
            {
                Console.WriteLine($"LiveRemoraInteractionContext: defer skipped (already deferred). InteractionId={m_interactionId.Value}.");
                return;
            }

            InteractionResponse response = new(InteractionCallbackType.DeferredChannelMessageWithSource, default);
            var result = await m_interactionApi.CreateInteractionResponseAsync(m_interactionId, m_token, response, default, m_cancellationToken);
            if (!result.IsSuccess)
            {
                string errorText = result.Error?.ToString() ?? string.Empty;
                if (errorText.Contains("InteractionHasAlreadyBeenAcknowledged", StringComparison.Ordinal))
                {
                    IsDeferred = true;
                    Console.WriteLine($"LiveRemoraInteractionContext: defer skipped because interaction was already acknowledged. InteractionId={m_interactionId.Value}.");
                    return;
                }

                Console.Error.WriteLine($"LiveRemoraInteractionContext: defer failed. InteractionId={m_interactionId.Value}. {result.Error}");
                throw new InvalidOperationException($"Failed to defer interaction response: {result.Error}");
            }

            IsDeferred = true;
            Console.WriteLine($"LiveRemoraInteractionContext: defer succeeded. InteractionId={m_interactionId.Value}.");
        }

        public async Task EditResponseAsync(IBotWebhookBuilder webhookBuilder)
        {
            Console.WriteLine($"LiveRemoraInteractionContext: edit response requested. InteractionId={m_interactionId.Value}, IsDeferred={IsDeferred}.");

            if (webhookBuilder is not RemoraWebhookBuilder remoraWebhookBuilder)
            {
                Console.Error.WriteLine($"LiveRemoraInteractionContext: edit response rejected due to webhook builder type mismatch. InteractionId={m_interactionId.Value}.");
                throw new InvalidOperationException("Passed an incorrect webhook builder type");
            }

            if (!IsDeferred)
            {
                Console.WriteLine($"LiveRemoraInteractionContext: edit requested before defer; auto-deferring. InteractionId={m_interactionId.Value}.");
                await DeferInteractionResponse();
            }

            var result = await m_interactionApi.EditOriginalInteractionResponseAsync(
                m_applicationId,
                m_token,
                new Optional<string?>(remoraWebhookBuilder.Content),
                default,
                default,
                default,
                default,
                default,
                m_cancellationToken);

            if (!result.IsSuccess)
            {
                Console.Error.WriteLine($"LiveRemoraInteractionContext: edit response failed. InteractionId={m_interactionId.Value}. {result.Error}");
                throw new InvalidOperationException($"Failed to edit interaction response: {result.Error}");
            }

            Console.WriteLine($"LiveRemoraInteractionContext: edit response succeeded. InteractionId={m_interactionId.Value}.");
        }

        public async Task UpdateOriginalMessageAsync(IInteractionResponseBuilder builder)
        {
            Console.WriteLine($"LiveRemoraInteractionContext: update original message requested. InteractionId={m_interactionId.Value}.");

            if (builder is not RemoraInteractionResponseBuilder remoraBuilder)
            {
                Console.Error.WriteLine($"LiveRemoraInteractionContext: update original message rejected due to builder type mismatch. InteractionId={m_interactionId.Value}.");
                throw new InvalidOperationException("Passed an incorrect interaction response builder type");
            }

            InteractionMessageCallbackData callbackData = new(
                default,
                new Optional<string>(remoraBuilder.Content),
                default,
                default,
                default,
                BuildMessageComponentRows(remoraBuilder.Components),
                default);

            InteractionResponse response = new(
                InteractionCallbackType.UpdateMessage,
                new Optional<OneOf<IInteractionMessageCallbackData, IInteractionAutocompleteCallbackData, IInteractionModalCallbackData>>(callbackData));

            var result = await m_interactionApi.CreateInteractionResponseAsync(
                m_interactionId,
                m_token,
                response,
                default,
                m_cancellationToken);

            if (!result.IsSuccess)
            {
                Console.Error.WriteLine($"LiveRemoraInteractionContext: update original message failed. InteractionId={m_interactionId.Value}. {result.Error}");
                throw new InvalidOperationException($"Failed to update original interaction message: {result.Error}");
            }

            IsDeferred = true;
            Console.WriteLine($"LiveRemoraInteractionContext: update original message succeeded. InteractionId={m_interactionId.Value}.");
        }

        public async Task ShowModalAsync(IInteractionResponseBuilder builder)
        {
            Console.WriteLine($"LiveRemoraInteractionContext: show modal requested. InteractionId={m_interactionId.Value}, IsDeferred={IsDeferred}.");

            if (builder is not RemoraInteractionResponseBuilder remoraBuilder)
            {
                Console.Error.WriteLine($"LiveRemoraInteractionContext: show modal rejected due to builder type mismatch. InteractionId={m_interactionId.Value}.");
                throw new InvalidOperationException("Passed an incorrect interaction response builder type");
            }

            if (IsDeferred)
            {
                Console.Error.WriteLine($"LiveRemoraInteractionContext: show modal rejected because interaction already acknowledged. InteractionId={m_interactionId.Value}.");
                throw new InvalidOperationException("Cannot show a modal after the interaction has already been acknowledged.");
            }

            if (string.IsNullOrWhiteSpace(remoraBuilder.CustomId))
            {
                Console.Error.WriteLine($"LiveRemoraInteractionContext: show modal rejected because custom ID is missing. InteractionId={m_interactionId.Value}.");
                throw new InvalidOperationException("Modal custom ID is required.");
            }

            IReadOnlyList<IMessageComponent> modalRows = BuildModalRows(remoraBuilder.Components);
            if (modalRows.Count == 0)
            {
                Console.Error.WriteLine($"LiveRemoraInteractionContext: show modal rejected because modal rows are empty. InteractionId={m_interactionId.Value}.");
                throw new InvalidOperationException("Modal must include at least one text input component.");
            }

            string modalTitle = string.IsNullOrWhiteSpace(remoraBuilder.Title) ? "Modal" : remoraBuilder.Title;
            InteractionModalCallbackData callbackData = new(remoraBuilder.CustomId, modalTitle, modalRows, default);
            InteractionResponse response = new(
                InteractionCallbackType.Modal,
                new Optional<OneOf<IInteractionMessageCallbackData, IInteractionAutocompleteCallbackData, IInteractionModalCallbackData>>(callbackData));

            var result = await m_interactionApi.CreateInteractionResponseAsync(
                m_interactionId,
                m_token,
                response,
                default,
                m_cancellationToken);

            if (!result.IsSuccess)
            {
                Console.Error.WriteLine($"LiveRemoraInteractionContext: show modal failed. InteractionId={m_interactionId.Value}. {result.Error}");
                throw new InvalidOperationException($"Failed to show modal: {result.Error}");
            }

            // Modal responses acknowledge the interaction and prevent further defer/respond attempts.
            IsDeferred = true;
            Console.WriteLine($"LiveRemoraInteractionContext: show modal succeeded. InteractionId={m_interactionId.Value}, CustomId='{remoraBuilder.CustomId}'.");
        }

        private static Optional<IReadOnlyList<IMessageComponent>> BuildMessageComponentRows(IReadOnlyList<IBotComponent> components)
        {
            IReadOnlyList<IMessageComponent> mapped = MapComponents(components, forModal: false);
            if (mapped.Count == 0)
            {
                return default;
            }

            return new Optional<IReadOnlyList<IMessageComponent>>(new[] { new ActionRowComponent(mapped, default) });
        }

        private static IReadOnlyList<IMessageComponent> BuildModalRows(IReadOnlyList<IBotComponent> components)
        {
            IReadOnlyList<IMessageComponent> mapped = MapComponents(components, forModal: true);
            return mapped
                .Select(component => (IMessageComponent)new ActionRowComponent(new[] { component }, default))
                .ToArray();
        }

        private static IReadOnlyList<IMessageComponent> MapComponents(IReadOnlyList<IBotComponent> components, bool forModal)
        {
            List<IMessageComponent> mapped = new();
            foreach (IBotComponent component in components)
            {
                if (component is not RemoraComponent remoraComponent)
                {
                    continue;
                }

                IMessageComponent? messageComponent = remoraComponent.Kind switch
                {
                    RemoraComponent.ComponentKind.Button when !forModal => BuildButton(remoraComponent),
                    RemoraComponent.ComponentKind.SelectMenu when !forModal => BuildSelectMenu(remoraComponent),
                    RemoraComponent.ComponentKind.TextInput when forModal => BuildTextInput(remoraComponent),
                    _ => null,
                };

                if (messageComponent is not null)
                {
                    mapped.Add(messageComponent);
                }
            }

            return mapped;
        }

        private static ButtonComponent BuildButton(RemoraComponent component)
        {
            Optional<IPartialEmoji> emoji = string.IsNullOrWhiteSpace(component.Emoji)
                ? default
                : new Optional<IPartialEmoji>(new Emoji(default, component.Emoji, default, default, default, default, default, default));

            return new ButtonComponent(
                MapButtonStyle(component.ButtonType),
                new Optional<string>(component.Label),
                emoji,
                new Optional<string>(component.CustomId),
                default,
                new Optional<bool>(component.Disabled),
                default);
        }

        private static StringSelectComponent BuildSelectMenu(RemoraComponent component)
        {
            IReadOnlyList<ISelectOption> options = component.SelectOptions
                .Select(option => (ISelectOption)new SelectOption(
                    option.Label,
                    option.Value,
                    string.IsNullOrWhiteSpace(option.Description) ? default : new Optional<string>(option.Description),
                    string.IsNullOrWhiteSpace(option.Emoji)
                        ? default
                        : new Optional<IPartialEmoji>(new Emoji(default, option.Emoji, default, default, default, default, default, default)),
                    new Optional<bool>(option.IsDefault)))
                .ToArray();

            return new StringSelectComponent(
                component.CustomId,
                options,
                string.IsNullOrWhiteSpace(component.Placeholder) ? default : new Optional<string>(component.Placeholder),
                new Optional<int>(component.MinOptions),
                new Optional<int>(component.MaxOptions),
                new Optional<bool>(component.Disabled),
                default);
        }

        private static TextInputComponent BuildTextInput(RemoraComponent component)
        {
            return new TextInputComponent(
                component.CustomId,
                TextInputStyle.Short,
                component.Label,
                default,
                default,
                new Optional<bool>(component.Required),
                string.IsNullOrWhiteSpace(component.Value) ? default : new Optional<string>(component.Value),
                string.IsNullOrWhiteSpace(component.Placeholder) ? default : new Optional<string>(component.Placeholder),
                default);
        }

        private static ButtonComponentStyle MapButtonStyle(IBotSystem.ButtonType type)
        {
            return type switch
            {
                IBotSystem.ButtonType.Primary => ButtonComponentStyle.Primary,
                IBotSystem.ButtonType.Secondary => ButtonComponentStyle.Secondary,
                IBotSystem.ButtonType.Success => ButtonComponentStyle.Success,
                IBotSystem.ButtonType.Danger => ButtonComponentStyle.Danger,
                _ => ButtonComponentStyle.Primary,
            };
        }
    }
}
