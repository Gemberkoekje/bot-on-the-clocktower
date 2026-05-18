using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Remora
{
    public class RemoraInteractionContext : IBotInteractionContext
    {
        public IGuild Guild { get; }

        public IChannel Channel { get; }

        public IMember Member { get; }

        public string? ComponentCustomId { get; }

        public IEnumerable<string> ComponentValues { get; }

        public bool IsDeferred { get; private set; }

        public IBotWebhookBuilder? LastWebhookBuilder { get; private set; }

        public IInteractionResponseBuilder? LastInteractionResponseBuilder { get; private set; }

        public RemoraInteractionContext(IGuild guild, IChannel channel, IMember member, string? componentCustomId = null, IEnumerable<string>? componentValues = null)
        {
            Guild = guild;
            Channel = channel;
            Member = member;
            ComponentCustomId = componentCustomId;
            ComponentValues = componentValues ?? Enumerable.Empty<string>();
        }

        public Task DeferInteractionResponse()
        {
            IsDeferred = true;
            return Task.CompletedTask;
        }

        public Task EditResponseAsync(IBotWebhookBuilder webhookBuilder)
        {
            if (webhookBuilder is not RemoraWebhookBuilder)
            {
                throw new InvalidOperationException("Passed an incorrect webhook builder type");
            }

            LastWebhookBuilder = webhookBuilder;
            return Task.CompletedTask;
        }

        public Task UpdateOriginalMessageAsync(IInteractionResponseBuilder builder)
        {
            if (builder is not RemoraInteractionResponseBuilder)
            {
                throw new InvalidOperationException("Passed an incorrect interaction response builder type");
            }

            LastInteractionResponseBuilder = builder;
            return Task.CompletedTask;
        }

        public Task ShowModalAsync(IInteractionResponseBuilder builder)
        {
            if (builder is not RemoraInteractionResponseBuilder)
            {
                throw new InvalidOperationException("Passed an incorrect interaction response builder type");
            }

            LastInteractionResponseBuilder = builder;
            return Task.CompletedTask;
        }
    }
}
