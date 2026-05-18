using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bot.Api;

namespace Bot.Remora
{
    public sealed class RemoraMessagingSlashCommands : IRemoraSlashCommandSource
    {
        private readonly IBotMessaging m_messaging;

        public RemoraMessagingSlashCommands(IBotMessaging messaging)
        {
            m_messaging = messaging;
        }

        public IEnumerable<IRemoraSlashCommand> GetCommands()
        {
            yield return new EvilCommand(m_messaging);
            yield return new LunaticCommand(m_messaging);
        }

        private sealed class EvilCommand : IRemoraSlashCommand
        {
            private readonly IBotMessaging m_messaging;

            public EvilCommand(IBotMessaging messaging) { m_messaging = messaging; }

            public string Name => "evil";
            public string Description => "Send a message informing the evil team of each other";
            public IReadOnlyList<RemoraSlashCommandParameter> Parameters { get; } = new[]
            {
                new RemoraSlashCommandParameter("demon", "The demon for this game", RemoraSlashCommandParameterType.User, true),
                new RemoraSlashCommandParameter("minion1", "A minion for this game", RemoraSlashCommandParameterType.User, true),
                new RemoraSlashCommandParameter("minion2", "A minion for this game (optional)", RemoraSlashCommandParameterType.User, false),
                new RemoraSlashCommandParameter("minion3", "A minion for this game (optional)", RemoraSlashCommandParameterType.User, false),
                new RemoraSlashCommandParameter("magician", "If a Magician is in this game, specify them here (optional)", RemoraSlashCommandParameterType.User, false),
            };

            public Task InvokeAsync(IBotInteractionContext context, IReadOnlyDictionary<string, object> arguments)
            {
                IMember demon = arguments.GetRequired<IMember>("demon");
                IMember minion1 = arguments.GetRequired<IMember>("minion1");
                var minions = new[] { minion1, arguments.GetOptional<IMember>("minion2")!, arguments.GetOptional<IMember>("minion3")! }
                    .Where(m => m != null).ToList();
                IMember? magician = arguments.GetOptional<IMember>("magician");
                return m_messaging.CommandEvilMessageAsync(context, demon, minions, magician);
            }
        }

        private sealed class LunaticCommand : IRemoraSlashCommand
        {
            private readonly IBotMessaging m_messaging;

            public LunaticCommand(IBotMessaging messaging) { m_messaging = messaging; }

            public string Name => "lunatic";
            public string Description => "Send a message to the Lunatic that *looks* like they're the demon";
            public IReadOnlyList<RemoraSlashCommandParameter> Parameters { get; } = new[]
            {
                new RemoraSlashCommandParameter("lunatic", "The Lunatic!", RemoraSlashCommandParameterType.User, true),
                new RemoraSlashCommandParameter("fakeMinion1", "A fake minion player", RemoraSlashCommandParameterType.User, true),
                new RemoraSlashCommandParameter("fakeMinion2", "A fake minion player", RemoraSlashCommandParameterType.User, false),
                new RemoraSlashCommandParameter("fakeMinion3", "A fake minion player", RemoraSlashCommandParameterType.User, false),
            };

            public Task InvokeAsync(IBotInteractionContext context, IReadOnlyDictionary<string, object> arguments)
            {
                IMember lunatic = arguments.GetRequired<IMember>("lunatic");
                IMember fake1 = arguments.GetRequired<IMember>("fakeMinion1");
                var minions = new[] { fake1, arguments.GetOptional<IMember>("fakeMinion2")!, arguments.GetOptional<IMember>("fakeMinion3")! }
                    .Where(m => m != null).ToList();
                return m_messaging.CommandLunaticMessageAsync(context, lunatic, minions);
            }
        }
    }
}
