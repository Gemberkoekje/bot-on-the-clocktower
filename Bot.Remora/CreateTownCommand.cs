using System.ComponentModel;
using System.Threading.Tasks;
using Bot.Api;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using DiscordRole = Remora.Discord.API.Abstractions.Objects.IRole;
using BotRole = Bot.Api.IRole;

namespace Bot.Remora
{
    /// <summary>
    /// Minimal Remora-native command group for setup workflow validation.
    /// Uses existing <see cref="IBotSetup"/> logic through the live interaction context.
    /// </summary>
    public sealed class CreateTownCommand : CommandGroup
    {
        private readonly IBotSetup m_setup;
        private readonly IInteractionContext m_interactionContext;
        private readonly ILiveRemoraInteractionContextFactory m_contextFactory;

        public CreateTownCommand(
            IBotSetup setup,
            IInteractionContext interactionContext,
            ILiveRemoraInteractionContextFactory contextFactory)
        {
            m_setup = setup;
            m_interactionContext = interactionContext;
            m_contextFactory = contextFactory;
        }

        [Command("createtown")]
        [Description("Create a new Town on this server")]
        public async Task<IResult> HandleCreateTownAsync(
            [Description("Town Name")] string townName,
            [Description("Server Player Role - only they can see the town")] DiscordRole playerRole = null,
            [Description("Server Storyteller Role - only they can see control channels")] DiscordRole storytellerRole = null,
            [Description("If true, a Night category full of cottages will be created")] bool useNight = true)
        {
            IInteraction interaction = m_interactionContext.Interaction;
            IBotInteractionContext context = m_contextFactory.Create(interaction);

            BotRole player = playerRole is null ? null : new ResolvedRoleAdapter(playerRole);
            BotRole storyteller = storytellerRole is null ? null : new ResolvedRoleAdapter(storytellerRole);

            await m_setup.CreateTownAsync(context, townName, player, storyteller, useNight);
            return Result.FromSuccess();
        }

    }
}
