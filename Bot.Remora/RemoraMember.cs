using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace Bot.Remora
{
    public class RemoraMember : IMember
    {
        public string DisplayName { get; private set; }

        public bool IsBot { get; }

        public ulong Id { get; }

        public IReadOnlyCollection<IRole> Roles => m_roles;

        private readonly List<IRole> m_roles;
        private readonly IDiscordRestUserAPI? m_userApi;
        private readonly IDiscordRestChannelAPI? m_channelApi;

        public IChannel? CurrentVoiceChannel { get; private set; }

        public RemoraMember(
            ulong id,
            string displayName,
            bool isBot = false,
            IEnumerable<IRole>? roles = null,
            IDiscordRestUserAPI? userApi = null,
            IDiscordRestChannelAPI? channelApi = null)
        {
            Id = id;
            DisplayName = displayName;
            IsBot = isBot;
            m_roles = roles != null ? new List<IRole>(roles) : new List<IRole>();
            m_userApi = userApi;
            m_channelApi = channelApi;
        }

        public Task MoveToChannelAsync(IChannel c)
        {
            CurrentVoiceChannel = c;
            return Task.CompletedTask;
        }

        public Task GrantRoleAsync(IRole role)
        {
            if (m_roles.All(r => r.Id != role.Id))
            {
                m_roles.Add(role);
            }
            return Task.CompletedTask;
        }

        public Task RevokeRoleAsync(IRole role)
        {
            m_roles.RemoveAll(r => r.Id == role.Id);
            return Task.CompletedTask;
        }

        public async Task<IMessage> SendMessageAsync(string content)
        {
            if (m_userApi is null || m_channelApi is null)
            {
                throw new InvalidOperationException("Cannot send direct message because Discord REST APIs are unavailable.");
            }

            var dmResult = await m_userApi.CreateDMAsync(new Snowflake(Id)).ConfigureAwait(false);
            if (!dmResult.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to create DM channel for member {Id}: {dmResult.Error}");
            }

            var msgResult = await m_channelApi.CreateMessageAsync(
                dmResult.Entity.ID,
                new Optional<string>(content),
                ct: default).ConfigureAwait(false);
            if (!msgResult.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to send DM to member {Id}: {msgResult.Error}");
            }

            return new RemoraMessage(content);
        }

        public Task SetDisplayName(string name)
        {
            DisplayName = name;
            return Task.CompletedTask;
        }
    }
}
