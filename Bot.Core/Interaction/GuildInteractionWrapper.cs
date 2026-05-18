using Bot.Api;

namespace Bot.Core.Interaction
{
    public class GuildInteractionWrapper : BaseInteractionWrapper<ulong, IGuildInteractionQueue, IGuildInteractionErrorHandler>, IGuildInteractionWrapper
    {
        public GuildInteractionWrapper(IGuildInteractionQueue queue, IGuildInteractionErrorHandler errorHandler)
            : base(queue, errorHandler)
        {
        }

        protected override ulong KeyFromContext(IBotInteractionContext context) => context.Guild.Id;
    }
}
