using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bot.Api;

namespace Bot.Remora
{
    public sealed class RemoraGameSlashCommands : IRemoraSlashCommandSource
    {
        private readonly IBotGameplayInteractionHandler m_handler;

        public RemoraGameSlashCommands(IBotGameplayInteractionHandler handler)
        {
            m_handler = handler;
        }

        public IEnumerable<IRemoraSlashCommand> GetCommands()
        {
            yield return new SimpleCommand("game", "Starts up a game of Blood on the Clocktower", m_handler.CommandGameAsync);
            yield return new SimpleCommand("night", "Move all active players from Town Square into Cottages for the night", m_handler.CommandNightAsync);
            yield return new SimpleCommand("day", "Move all active players from Cottages to Town Square", m_handler.CommandDayAsync);
            yield return new SimpleCommand("vote", "Move all active players to Town Square for voting", m_handler.CommandVoteAsync);
            yield return new VoteTimerCommand(m_handler);
            yield return new SimpleCommand("stopVoteTimer", "Cancels an outstanding call to `/voteTimer`.", m_handler.RunStopVoteTimerAsync);
            yield return new SimpleCommand("endGame", "End any current game, removing roles etc", m_handler.CommandEndGameAsync);
            yield return new StorytellersCommand(m_handler);
        }

        private sealed class SimpleCommand : IRemoraSlashCommand
        {
            private readonly System.Func<IBotInteractionContext, Task> m_callback;

            public SimpleCommand(string name, string description, System.Func<IBotInteractionContext, Task> callback)
            {
                Name = name;
                Description = description;
                m_callback = callback;
            }

            public string Name { get; }
            public string Description { get; }
            public IReadOnlyList<RemoraSlashCommandParameter> Parameters { get; } = System.Array.Empty<RemoraSlashCommandParameter>();

            public Task InvokeAsync(IBotInteractionContext context, IReadOnlyDictionary<string, object> arguments) => m_callback(context);
        }

        private sealed class VoteTimerCommand : IRemoraSlashCommand
        {
            private readonly IBotGameplayInteractionHandler m_handler;

            public VoteTimerCommand(IBotGameplayInteractionHandler handler)
            {
                m_handler = handler;
            }

            public string Name => "voteTimer";
            public string Description => "Move all active players to Town Square for voting after a provided amount of time";
            public IReadOnlyList<RemoraSlashCommandParameter> Parameters { get; } = new[]
            {
                new RemoraSlashCommandParameter("timeString", "Time string, such as \"5m30s\" or \"2 minutes\". Valid times are between 10 seconds and 20 minutes.", RemoraSlashCommandParameterType.String, true),
            };

            public Task InvokeAsync(IBotInteractionContext context, IReadOnlyDictionary<string, object> arguments)
            {
                string timeString = arguments.GetRequired<string>("timeString");
                return m_handler.RunVoteTimerAsync(context, timeString);
            }
        }

        private sealed class StorytellersCommand : IRemoraSlashCommand
        {
            private readonly IBotGameplayInteractionHandler m_handler;

            public StorytellersCommand(IBotGameplayInteractionHandler handler)
            {
                m_handler = handler;
            }

            public string Name => "storytellers";
            public string Description => "Explicitly list which users should be Storytellers";
            public IReadOnlyList<RemoraSlashCommandParameter> Parameters { get; } = new[]
            {
                new RemoraSlashCommandParameter("user1", "Storyteller", RemoraSlashCommandParameterType.User, true),
                new RemoraSlashCommandParameter("user2", "Further storyteller", RemoraSlashCommandParameterType.User, false),
                new RemoraSlashCommandParameter("user3", "An additional storyteller", RemoraSlashCommandParameterType.User, false),
                new RemoraSlashCommandParameter("user4", "Yet another storyteller", RemoraSlashCommandParameterType.User, false),
                new RemoraSlashCommandParameter("user5", "Hopefully the last storyteller", RemoraSlashCommandParameterType.User, false),
            };

            public Task InvokeAsync(IBotInteractionContext context, IReadOnlyDictionary<string, object> arguments)
            {
                var members = new[] { "user1", "user2", "user3", "user4", "user5" }
                    .Select(k => arguments.GetOptional<IMember>(k))
                    .Where(m => m != null)
                    .Cast<IMember>()
                    .ToList();
                return m_handler.CommandSetStorytellersAsync(context, members);
            }
        }
    }
}
