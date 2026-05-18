using Bot.Api;

namespace Bot.Core.Interaction
{
    public class GuildInteractionErrorHandler : BaseInteractionErrorHandler<ulong>, IGuildInteractionErrorHandler
    {
        public GuildInteractionErrorHandler(IProcessLoggerFactory processLoggerFactory, ITask task)
            : base(processLoggerFactory, task)
        {
        }

        protected override string GetFriendlyStringForKey(ulong key) => $"Guild: `{key}`";
    }
}
