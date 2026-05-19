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
            Console.WriteLine($"RemoraInteractionResponder: begin. InteractionId={interaction.ID.Value}, Type={interaction.Type}, ApplicationId={interaction.ApplicationID.Value}, GuildId={(interaction.GuildID.HasValue ? interaction.GuildID.Value.Value : 0UL)}.");

            try
            {
                switch (interaction.Type)
                {
                    case InteractionType.ApplicationCommand:
                        Console.WriteLine($"RemoraInteractionResponder: routing to slash dispatcher. InteractionId={interaction.ID.Value}.");
                        await m_slashDispatcher.DispatchAsync(interaction, cancellationToken);
                        Console.WriteLine($"RemoraInteractionResponder: slash dispatch completed. InteractionId={interaction.ID.Value}.");
                        return;

                    case InteractionType.MessageComponent:
                    case InteractionType.ModalSubmit:
                        Console.WriteLine($"RemoraInteractionResponder: routing to component dispatcher. InteractionId={interaction.ID.Value}, InteractionType={interaction.Type}.");
                        bool wasHandled = await m_componentDispatcher.DispatchAsync(interaction, cancellationToken);
                        Console.WriteLine($"RemoraInteractionResponder: component dispatcher returned. InteractionId={interaction.ID.Value}, WasHandled={wasHandled}.");
                        if (!wasHandled)
                        {
                            Console.WriteLine($"RemoraInteractionResponder: sending unknown-component fallback. InteractionId={interaction.ID.Value}.");
                            await SendUnknownComponentResponseAsync(interaction, cancellationToken);
                        }
                        return;

                    default:
                        Console.WriteLine($"RemoraInteractionResponder: ignored interaction type. InteractionId={interaction.ID.Value}, Type={interaction.Type}.");
                        return;
                }
            }
            catch (Exception error)
            {
                Console.Error.WriteLine($"RemoraInteractionResponder: dispatch failed. InteractionId={interaction.ID.Value}, Type={interaction.Type}. {error}");
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
                    Console.Error.WriteLine($"RemoraInteractionResponder: failed unknown-component response. InteractionId={interaction.ID.Value}. {callbackResult.Error}");
                }
                else
                {
                    Console.WriteLine($"RemoraInteractionResponder: unknown-component fallback sent. InteractionId={interaction.ID.Value}.");
                }
            }
            catch (Exception error)
            {
                Console.Error.WriteLine($"RemoraInteractionResponder: failed to send unknown-component response. InteractionId={interaction.ID.Value}. {error}");
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
                    Console.WriteLine($"RemoraInteractionResponder: sent primary error response callback. InteractionId={interaction.ID.Value}.");
                    return;
                }

                Console.Error.WriteLine($"RemoraInteractionResponder: primary error callback failed; attempting follow-up. InteractionId={interaction.ID.Value}. {callbackResult.Error}");

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
                Console.Error.WriteLine($"RemoraInteractionResponder: failed to send error response. InteractionId={interaction.ID.Value}. {error}");
            }
        }
    }
}
