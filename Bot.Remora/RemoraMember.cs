using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Remora
{
    public class RemoraMember : IMember
    {
        public string DisplayName { get; private set; }

        public bool IsBot { get; }

        public ulong Id { get; }

        public IReadOnlyCollection<IRole> Roles => m_roles;

        private readonly List<IRole> m_roles;

        public IChannel? CurrentVoiceChannel { get; private set; }

        public RemoraMember(ulong id, string displayName, bool isBot = false, IEnumerable<IRole>? roles = null)
        {
            Id = id;
            DisplayName = displayName;
            IsBot = isBot;
            m_roles = roles != null ? new List<IRole>(roles) : new List<IRole>();
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

        public Task<IMessage> SendMessageAsync(string content)
        {
            return Task.FromResult<IMessage>(new RemoraMessage(content));
        }

        public Task SetDisplayName(string name)
        {
            DisplayName = name;
            return Task.CompletedTask;
        }
    }
}
