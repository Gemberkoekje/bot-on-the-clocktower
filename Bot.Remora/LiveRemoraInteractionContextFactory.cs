using System.Threading;
using Bot.Api;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;

namespace Bot.Remora
{
    public interface ILiveRemoraInteractionContextFactory
    {
        IBotInteractionContext Create(IInteraction interaction, CancellationToken cancellationToken = default);
    }

    public sealed class LiveRemoraInteractionContextFactory : ILiveRemoraInteractionContextFactory
    {
        private readonly IDiscordRestGuildAPI m_guildApi;
        private readonly IDiscordRestChannelAPI m_channelApi;
        private readonly IDiscordRestUserAPI m_userApi;
        private readonly IDiscordRestInteractionAPI m_interactionApi;

        public LiveRemoraInteractionContextFactory(
            IDiscordRestGuildAPI guildApi,
            IDiscordRestChannelAPI channelApi,
            IDiscordRestUserAPI userApi,
            IDiscordRestInteractionAPI interactionApi)
        {
            m_guildApi = guildApi;
            m_channelApi = channelApi;
            m_userApi = userApi;
            m_interactionApi = interactionApi;
        }

        public IBotInteractionContext Create(IInteraction interaction, CancellationToken cancellationToken = default)
        {
            ulong guildId = interaction.GuildID.HasValue ? interaction.GuildID.Value.Value : 0UL;
            ulong channelId = interaction.Channel.HasValue && interaction.Channel.Value.ID.HasValue
                ? interaction.Channel.Value.ID.Value.Value
                : 0UL;

            string channelName = interaction.Channel.HasValue && interaction.Channel.Value.Name.HasValue
                ? interaction.Channel.Value.Name.Value ?? $"channel-{channelId}"
                : $"channel-{channelId}";

            ulong memberId = 0UL;
            string memberName = "unknown-member";
            if (interaction.Member.HasValue && interaction.Member.Value.User.HasValue)
            {
                memberId = interaction.Member.Value.User.Value.ID.Value;
                memberName = interaction.Member.Value.Nickname.HasValue
                    ? interaction.Member.Value.Nickname.Value ?? interaction.Member.Value.User.Value.Username ?? memberName
                    : interaction.Member.Value.User.Value.Username ?? memberName;
            }
            else if (interaction.User.HasValue)
            {
                memberId = interaction.User.Value.ID.Value;
                memberName = interaction.User.Value.Username ?? memberName;
            }

            string guildName = $"guild-{guildId}";
            RemoraGuild guild = new(guildId, guildName, m_guildApi, m_channelApi, m_userApi);
            RemoraChannel channel = new(channelId, channelName, channelApi: m_channelApi);
            RemoraMember member = new(memberId, memberName, userApi: m_userApi, channelApi: m_channelApi);

            return new LiveRemoraInteractionContext(
                guild,
                channel,
                member,
                interactionApi: m_interactionApi,
                applicationId: interaction.ApplicationID,
                interactionId: interaction.ID,
                token: interaction.Token,
                cancellationToken: cancellationToken);
        }
    }
}
