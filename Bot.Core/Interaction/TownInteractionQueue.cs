using Bot.Api;

namespace Bot.Core.Interaction
{
    public class TownInteractionQueue : BaseInteractionQueue<TownKey>, ITownInteractionQueue
    {
        public TownInteractionQueue(IBotSystem botSystem, IShutdownPreventionService shutdownPreventionService)
            : base(botSystem, shutdownPreventionService)
        {
        }

        protected override TownKey KeyFromContext(IBotInteractionContext context) => context.GetTownKey();
        protected override string GetFriendlyStringForKey(TownKey townKey) => $"Guild: `{townKey.GuildId}`\nChannel: `{townKey.ControlChannelId}`";
    }
}
