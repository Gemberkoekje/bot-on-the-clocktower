using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bot.Api;

namespace Bot.Remora
{
    public sealed class RemoraMiscSlashCommands : IRemoraSlashCommandSource
    {
        private readonly IAnnouncer m_announcer;

        public RemoraMiscSlashCommands(IAnnouncer announcer)
        {
            m_announcer = announcer;
        }

        public IEnumerable<IRemoraSlashCommand> GetCommands()
        {
            yield return new AnnounceCommand(m_announcer);
        }

        private sealed class AnnounceCommand : IRemoraSlashCommand
        {
            private readonly IAnnouncer m_announcer;

            public AnnounceCommand(IAnnouncer announcer) { m_announcer = announcer; }

            public string Name => "announce";
            public string Description => "Set whether this server wants to hear new version announcements";
            public IReadOnlyList<RemoraSlashCommandParameter> Parameters { get; } = new[]
            {
                new RemoraSlashCommandParameter("hearAnnouncements", "If true, this server will hear new version announcements", RemoraSlashCommandParameterType.Boolean, true),
            };

            public Task InvokeAsync(IBotInteractionContext context, IReadOnlyDictionary<string, object> arguments)
            {
                bool hear = arguments.GetBool("hearAnnouncements", false);
                return m_announcer.CommandSetGuildAnnounce(context, hear);
            }
        }
    }
}
