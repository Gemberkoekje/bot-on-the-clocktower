using System;
using System.Threading;
using System.Threading.Tasks;
using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;

namespace Bot.Remora
{
    internal sealed class RemoraInteractionResponder : IRemoraInteractionResponder
    {
        private const string ErrorMessage = "Sorry, something went wrong while processing that command.";
        private const string UnknownComponentMessage = "Sorry, that interaction is no longer available.";

        private readonly IRemoraSlashCommandDispatcher m_slashDispatcher;
        private readonly IRemoraComponentDispatcher m_componentDispatcher;
        private readonly IDiscordRestInteractionAPI m_interactionApi;

        public RemoraInteractionResponder(
            IRemoraSlashCommandDispatcher slashDispatcher,
            IRemoraComponentDispatcher componentDispatcher,
            IDiscordRestInteractionAPI interactionApi)
        {
            m_slashDispatcher = slashDispatcher;
            m_componentDispatcher = componentDispatcher;
            m_interactionApi = interactionApi;
        }

        public async Task RespondAsync(IInteraction interaction, CancellationToken cancellationToken = default)
        {
            try
            {
                switch (interaction.Type)
                {
                    case InteractionType.ApplicationCommand:
                        await m_slashDispatcher.DispatchAsync(interaction, cancellationToken);
                        return;

                    case InteractionType.MessageComponent:
                    case InteractionType.ModalSubmit:
                        bool wasHandled = await m_componentDispatcher.DispatchAsync(interaction, cancellationToken);
                        if (!wasHandled)
                        {
                            await SendUnknownComponentResponseAsync(interaction, cancellationToken);
                        }
                        return;

                    default:
                        Console.WriteLine($"RemoraInteractionResponder: ignored interaction type {interaction.Type}.");
                        return;
                }
            }
            catch (Exception error)
            {
                Console.Error.WriteLine($"RemoraInteractionResponder: dispatch failed for interaction {interaction.ID.Value}. {error}");
                await SendErrorResponseAsync(interaction, cancellationToken);
            }
        }

        private async Task SendUnknownComponentResponseAsync(IInteraction interaction, CancellationToken cancellationToken)
        {
            try
            {
                InteractionMessageCallbackData callbackData = new(
                    default,
                    new Optional<string>(UnknownComponentMessage),
                    default,
                    default,
                    new Optional<MessageFlags>(MessageFlags.Ephemeral),
                    default,
                    default);

                InteractionResponse callback = new(
                    InteractionCallbackType.ChannelMessageWithSource,
                    new Optional<OneOf<IInteractionMessageCallbackData, IInteractionAutocompleteCallbackData, IInteractionModalCallbackData>>(callbackData));

                Result callbackResult = await m_interactionApi.CreateInteractionResponseAsync(
                    interaction.ID,
                    interaction.Token,
                    callback,
                    default,
                    cancellationToken);

                if (!callbackResult.IsSuccess)
                {
                    Console.Error.WriteLine($"RemoraInteractionResponder: failed unknown-component response for interaction {interaction.ID.Value}. {callbackResult.Error}");
                }
            }
            catch (Exception error)
            {
                Console.Error.WriteLine($"RemoraInteractionResponder: failed to send unknown-component response for interaction {interaction.ID.Value}. {error}");
            }
        }

        private async Task SendErrorResponseAsync(IInteraction interaction, CancellationToken cancellationToken)
        {
            try
            {
                InteractionMessageCallbackData callbackData = new(
                    default,
                    new Optional<string>(ErrorMessage),
                    default,
                    default,
                    new Optional<MessageFlags>(MessageFlags.Ephemeral),
                    default,
                    default);

                InteractionResponse callback = new(
                    InteractionCallbackType.ChannelMessageWithSource,
                    new Optional<OneOf<IInteractionMessageCallbackData, IInteractionAutocompleteCallbackData, IInteractionModalCallbackData>>(callbackData));

                Result callbackResult = await m_interactionApi.CreateInteractionResponseAsync(
                    interaction.ID,
                    interaction.Token,
                    callback,
                    default,
                    cancellationToken);

                if (callbackResult.IsSuccess)
                {
                    return;
                }

                await m_interactionApi.CreateFollowupMessageAsync(
                    interaction.ApplicationID,
                    interaction.Token,
                    new Optional<string>(ErrorMessage),
                    default,
                    default,
                    default,
                    default,
                    default,
                    new Optional<MessageFlags>(MessageFlags.Ephemeral),
                    cancellationToken);
            }
            catch (Exception error)
            {
                Console.Error.WriteLine($"RemoraInteractionResponder: failed to send error response for interaction {interaction.ID.Value}. {error}");
            }
        }
    }
}
