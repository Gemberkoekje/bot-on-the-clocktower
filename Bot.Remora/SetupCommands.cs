using System.ComponentModel;
using System.Threading.Tasks;
using Bot.Api;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using DiscordChannel = Remora.Discord.API.Abstractions.Objects.IPartialChannel;
using DiscordRole = Remora.Discord.API.Abstractions.Objects.IRole;
using BotChannel = Bot.Api.IChannel;
using BotChannelCategory = Bot.Api.IChannelCategory;
using BotRole = Bot.Api.IRole;

namespace Bot.Remora
{
    public sealed class SetupCommands : CommandGroup
    {
        private readonly IBotSetup m_setup;
        private readonly IInteractionContext m_interactionContext;
        private readonly ILiveRemoraInteractionContextFactory m_contextFactory;

        public SetupCommands(
            IBotSetup setup,
            IInteractionContext interactionContext,
            ILiveRemoraInteractionContextFactory contextFactory)
        {
            m_setup = setup;
            m_interactionContext = interactionContext;
            m_contextFactory = contextFactory;
        }

        [Command("towninfo")]
        [Description("Get info about any town registered for this server & channel")]
        public async Task<IResult> HandleTownInfoAsync()
        {
            await m_setup.TownInfoAsync(CreateContext());
            return Result.FromSuccess();
        }

        [Command("destroytown")]
        [Description("Destroy any channels and roles created by /createtown for the town with the given name")]
        public async Task<IResult> HandleDestroyTownAsync([Description("Town Name")] string townName)
        {
            await m_setup.DestroyTownAsync(CreateContext(), townName);
            return Result.FromSuccess();
        }

        [Command("modifytown")]
        [Description("Modify one of the optional details of a town")]
        public async Task<IResult> HandleModifyTownAsync(
            [Description("Set the (text) chat channel for this town")] DiscordChannel chatChannel = null,
            [Description("Set the Night category for this town")] DiscordChannel nightCategory = null)
        {
            BotChannel chat = chatChannel is null ? null : new ResolvedChannelAdapter(chatChannel);
            BotChannelCategory nightCat = nightCategory is null ? null : new ResolvedChannelCategoryAdapter(nightCategory);

            await m_setup.ModifyTownAsync(CreateContext(), chat, nightCat);
            return Result.FromSuccess();
        }

        [Command("addtown")]
        [Description("Add a new town composed of existing channel and roles on this server")]
        public async Task<IResult> HandleAddTownAsync(
            [Description("Control channel (must be text)")] DiscordChannel controlChannel,
            [Description("Town Square channel (must be voice)")] DiscordChannel townSquare,
            [Description("Day Category (must contain control and town square channels)")] DiscordChannel dayCategory,
            [Description("Role to grant storytellers in this town during an active game")] DiscordRole storytellerRole,
            [Description("Role to grant villagers in this town during an active game")] DiscordRole villagerRole,
            [Description("Night Category (optional)")] DiscordChannel nightCategory = null,
            [Description("Chat channel (optional, must be text)")] DiscordChannel chatChannel = null)
        {
            BotChannel control = new ResolvedChannelAdapter(controlChannel);
            BotChannel square = new ResolvedChannelAdapter(townSquare);
            BotChannelCategory dayCat = new ResolvedChannelCategoryAdapter(dayCategory);
            BotRole stRole = new ResolvedRoleAdapter(storytellerRole);
            BotRole villager = new ResolvedRoleAdapter(villagerRole);
            BotChannelCategory nightCat = nightCategory is null ? null : new ResolvedChannelCategoryAdapter(nightCategory);
            BotChannel chat = chatChannel is null ? null : new ResolvedChannelAdapter(chatChannel);

            await m_setup.AddTownAsync(CreateContext(), control, square, dayCat, nightCat, stRole, villager, chat);
            return Result.FromSuccess();
        }

        [Command("removetown")]
        [Description("Unregister a town on this server without deleting any channels or roles")]
        public async Task<IResult> HandleRemoveTownAsync(
            [Description("Town to remove - if blank, must be run from the town's control channel")] DiscordChannel dayCategory = null)
        {
            BotChannelCategory category = dayCategory is null ? null : new ResolvedChannelCategoryAdapter(dayCategory);

            await m_setup.RemoveTownAsync(CreateContext(), category);
            return Result.FromSuccess();
        }

        private IBotInteractionContext CreateContext()
        {
            return m_contextFactory.Create(m_interactionContext.Interaction);
        }
    }
}
