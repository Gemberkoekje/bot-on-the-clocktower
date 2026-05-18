using Bot.Api;

namespace Bot.Core.Interaction
{
    public class GuildInteractionQueue : BaseInteractionQueue<ulong>, IGuildInteractionQueue
    {
        public GuildInteractionQueue(IBotSystem botSystem, IShutdownPreventionService shutdownPreventionService)
            : base(botSystem, shutdownPreventionService)
        {
        }

        protected override ulong KeyFromContext(IBotInteractionContext context) => context.Guild.Id;
        protected override string GetFriendlyStringForKey(ulong key) => $"Guild: `{key}`";
    }
}
