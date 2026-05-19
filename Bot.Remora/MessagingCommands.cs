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
    public sealed class MessagingCommands : CommandGroup
    {
        private readonly IBotMessaging m_messaging;
        private readonly IInteractionContext m_interactionContext;
        private readonly ILiveRemoraInteractionContextFactory m_contextFactory;

        public MessagingCommands(
            IBotMessaging messaging,
            IInteractionContext interactionContext,
            ILiveRemoraInteractionContextFactory contextFactory)
        {
            m_messaging = messaging;
            m_interactionContext = interactionContext;
            m_contextFactory = contextFactory;
        }

        [Command("evil")]
        [Description("Send a message informing the evil team of each other")]
        public async Task<IResult> HandleEvilAsync(
            [Description("The demon for this game")] IGuildMember demon,
            [Description("A minion for this game")] IGuildMember minion1,
            [Description("A minion for this game (optional)")] IGuildMember minion2 = null,
            [Description("A minion for this game (optional)")] IGuildMember minion3 = null,
            [Description("If a Magician is in this game, specify them here (optional)")] IGuildMember magician = null)
        {
            BotMember demonMember = ConvertMember(demon);
            List<BotMember> minions = new[] { minion1, minion2, minion3 }
                .Where(member => member != null)
                .Select(ConvertMember)
                .ToList();
            BotMember magicianMember = magician is null ? null : ConvertMember(magician);

            await m_messaging.CommandEvilMessageAsync(CreateContext(), demonMember, minions, magicianMember);
            return Result.FromSuccess();
        }

        [Command("lunatic")]
        [Description("Send a message to the Lunatic that *looks* like they're the demon")]
        public async Task<IResult> HandleLunaticAsync(
            [Description("The Lunatic!")] IGuildMember lunatic,
            [Description("A fake minion player")] IGuildMember fakeMinion1,
            [Description("A fake minion player")] IGuildMember fakeMinion2 = null,
            [Description("A fake minion player")] IGuildMember fakeMinion3 = null)
        {
            IMember lunaticMember = ConvertMember(lunatic);
            List<IMember> fakeMinions = new[] { fakeMinion1, fakeMinion2, fakeMinion3 }
                .Where(member => member != null)
                .Select(ConvertMember)
                .ToList();

            await m_messaging.CommandLunaticMessageAsync(CreateContext(), lunaticMember, fakeMinions);
            return Result.FromSuccess();
        }

        private IBotInteractionContext CreateContext()
        {
            return m_contextFactory.Create(m_interactionContext.Interaction);
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
    }
}
