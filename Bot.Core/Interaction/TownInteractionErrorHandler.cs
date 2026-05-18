using Bot.Api;

namespace Bot.Core.Interaction
{
    public class TownInteractionErrorHandler : BaseInteractionErrorHandler<TownKey>, ITownInteractionErrorHandler
    {
        public TownInteractionErrorHandler(IProcessLoggerFactory processLoggerFactory, ITask task)
            : base(processLoggerFactory, task)
        {
        }

        protected override string GetFriendlyStringForKey(TownKey townKey) => $"Guild: `{townKey.GuildId}`\nChannel: `{townKey.ControlChannelId}`";
    }
}
