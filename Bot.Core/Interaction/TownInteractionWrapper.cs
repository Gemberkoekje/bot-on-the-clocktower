using Bot.Api;

namespace Bot.Core.Interaction
{
    public class TownInteractionWrapper : BaseInteractionWrapper<TownKey, ITownInteractionQueue, ITownInteractionErrorHandler>, ITownInteractionWrapper
    {
        public TownInteractionWrapper(ITownInteractionQueue queue, ITownInteractionErrorHandler errorHandler)
            : base(queue, errorHandler)
        {
        }

        protected override TownKey KeyFromContext(IBotInteractionContext context) => context.GetTownKey();
    }
}
