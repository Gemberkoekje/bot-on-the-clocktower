using System.ComponentModel;
using System.Threading.Tasks;
using Bot.Api;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Bot.Remora
{
    public sealed class MiscCommands : CommandGroup
    {
        private readonly IAnnouncer m_announcer;
        private readonly IInteractionContext m_interactionContext;
        private readonly ILiveRemoraInteractionContextFactory m_contextFactory;

        public MiscCommands(
            IAnnouncer announcer,
            IInteractionContext interactionContext,
            ILiveRemoraInteractionContextFactory contextFactory)
        {
            m_announcer = announcer;
            m_interactionContext = interactionContext;
            m_contextFactory = contextFactory;
        }

        [Command("announce")]
        [Description("Set whether this server wants to hear new version announcements")]
        public async Task<IResult> HandleAnnounceAsync([Description("If true, this server will hear new version announcements")] bool hearAnnouncements)
        {
            await m_announcer.CommandSetGuildAnnounce(CreateContext(), hearAnnouncements);
            return Result.FromSuccess();
        }

        private IBotInteractionContext CreateContext()
        {
            return m_contextFactory.Create(m_interactionContext.Interaction);
        }
    }
}
