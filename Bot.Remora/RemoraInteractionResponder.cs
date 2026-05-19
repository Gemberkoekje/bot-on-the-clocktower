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

        private readonly IRemoraSlashCommandDispatcher m_dispatcher;
        private readonly IDiscordRestInteractionAPI m_interactionApi;

        public RemoraInteractionResponder(IRemoraSlashCommandDispatcher dispatcher, IDiscordRestInteractionAPI interactionApi)
        {
            m_dispatcher = dispatcher;
            m_interactionApi = interactionApi;
        }

        public async Task RespondAsync(IInteraction interaction, CancellationToken cancellationToken = default)
        {
            if (interaction.Type != InteractionType.ApplicationCommand)
            {
                Console.WriteLine($"RemoraInteractionResponder: ignored interaction type {interaction.Type}.");
                return;
            }

            try
            {
                await m_dispatcher.DispatchAsync(interaction, cancellationToken);
            }
            catch (Exception error)
            {
                Console.Error.WriteLine($"RemoraInteractionResponder: slash dispatch failed for interaction {interaction.ID.Value}. {error}");
                await SendErrorResponseAsync(interaction, cancellationToken);
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
