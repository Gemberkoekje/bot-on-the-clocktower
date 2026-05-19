using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bot.Api;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Abstractions.Objects;
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

        public Bot.Api.IGuild Guild { get; }

        public Bot.Api.IChannel Channel { get; }

        public IMember Member { get; }

        public string? ComponentCustomId => null;

        public IEnumerable<string> ComponentValues => Array.Empty<string>();

        public bool IsDeferred { get; private set; }

        public LiveRemoraInteractionContext(
            Bot.Api.IGuild guild,
            Bot.Api.IChannel channel,
            IMember member,
            IDiscordRestInteractionAPI interactionApi,
            Snowflake applicationId,
            Snowflake interactionId,
            string token,
            CancellationToken cancellationToken)
        {
            Guild = guild;
            Channel = channel;
            Member = member;
            m_interactionApi = interactionApi;
            m_applicationId = applicationId;
            m_interactionId = interactionId;
            m_token = token;
            m_cancellationToken = cancellationToken;
        }

        public async Task DeferInteractionResponse()
        {
            if (IsDeferred)
            {
                return;
            }

            InteractionResponse response = new(InteractionCallbackType.DeferredChannelMessageWithSource, default);
            var result = await m_interactionApi.CreateInteractionResponseAsync(m_interactionId, m_token, response, default, m_cancellationToken);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to defer interaction response: {result.Error}");
            }

            IsDeferred = true;
        }

        public async Task EditResponseAsync(IBotWebhookBuilder webhookBuilder)
        {
            if (webhookBuilder is not RemoraWebhookBuilder remoraWebhookBuilder)
            {
                throw new InvalidOperationException("Passed an incorrect webhook builder type");
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
                throw new InvalidOperationException($"Failed to edit interaction response: {result.Error}");
            }
        }

        public Task UpdateOriginalMessageAsync(IInteractionResponseBuilder builder)
        {
            throw new NotSupportedException("Updating original interaction messages is not yet supported.");
        }

        public Task ShowModalAsync(IInteractionResponseBuilder builder)
        {
            throw new NotSupportedException("Modal responses are not yet supported.");
        }
    }
}
