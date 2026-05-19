using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Bot.Api;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;
using Remora.Results;
using BotMember = Bot.Api.IMember;

namespace Bot.Remora
{
    public sealed class GameCommands : CommandGroup
    {
        private readonly IBotGameplayInteractionHandler m_handler;
        private readonly IInteractionContext m_interactionContext;
        private readonly ILiveRemoraInteractionContextFactory m_contextFactory;

        public GameCommands(
            IBotGameplayInteractionHandler handler,
            IInteractionContext interactionContext,
            ILiveRemoraInteractionContextFactory contextFactory)
        {
            m_handler = handler;
            m_interactionContext = interactionContext;
            m_contextFactory = contextFactory;
        }

        [Command("game")]
        [Description("Starts up a game of Blood on the Clocktower")]
        public async Task<IResult> HandleGameAsync()
        {
            await m_handler.CommandGameAsync(CreateContext());
            return Result.FromSuccess();
        }

        [Command("night")]
        [Description("Move all active players from Town Square into Cottages for the night")]
        public async Task<IResult> HandleNightAsync()
        {
            await m_handler.CommandNightAsync(CreateContext());
            return Result.FromSuccess();
        }

        [Command("day")]
        [Description("Move all active players from Cottages to Town Square")]
        public async Task<IResult> HandleDayAsync()
        {
            await m_handler.CommandDayAsync(CreateContext());
            return Result.FromSuccess();
        }

        [Command("vote")]
        [Description("Move all active players to Town Square for voting")]
        public async Task<IResult> HandleVoteAsync()
        {
            await m_handler.CommandVoteAsync(CreateContext());
            return Result.FromSuccess();
        }

        [Command("votetimer")]
        [Description("Move all active players to Town Square for voting after a provided amount of time")]
        public async Task<IResult> HandleVoteTimerAsync([Description("Time string, such as \"5m30s\" or \"2 minutes\". Valid times are between 10 seconds and 20 minutes.")] string timeString)
        {
            await m_handler.RunVoteTimerAsync(CreateContext(), timeString);
            return Result.FromSuccess();
        }

        [Command("stopvotetimer")]
        [Description("Cancels an outstanding call to /votetimer.")]
        public async Task<IResult> HandleStopVoteTimerAsync()
        {
            await m_handler.RunStopVoteTimerAsync(CreateContext());
            return Result.FromSuccess();
        }

        [Command("endgame")]
        [Description("End any current game, removing roles etc")]
        public async Task<IResult> HandleEndGameAsync()
        {
            await m_handler.CommandEndGameAsync(CreateContext());
            return Result.FromSuccess();
        }

        [Command("storytellers")]
        [Description("Explicitly list which users should be Storytellers")]
        public async Task<IResult> HandleStorytellersAsync(
            [Description("Storyteller")] IGuildMember user1,
            [Description("Further storyteller")] IGuildMember user2 = null,
            [Description("An additional storyteller")] IGuildMember user3 = null,
            [Description("Yet another storyteller")] IGuildMember user4 = null,
            [Description("Hopefully the last storyteller")] IGuildMember user5 = null)
        {
            List<BotMember> members = new[] { user1, user2, user3, user4, user5 }
                .Where(member => member != null)
                .Select(ConvertMember)
                .ToList();

            await m_handler.CommandSetStorytellersAsync(CreateContext(), members);
            return Result.FromSuccess();
        }

        private static BotMember ConvertMember(IGuildMember member)
        {
            if (!member.User.HasValue)
            {
                throw new InvalidOperationException("Guild member payload is missing required user information.");
            }

            return new ResolvedMemberAdapter(
                member.User.Value,
                member.Nickname,
                new Optional<IReadOnlyList<Snowflake>>(member.Roles),
                new Dictionary<Snowflake, global::Remora.Discord.API.Abstractions.Objects.IRole>());
        }

        private IBotInteractionContext CreateContext()
        {
            return m_contextFactory.Create(m_interactionContext.Interaction);
        }
    }
}
