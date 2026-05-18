using Bot.Api;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Remora
{
    public class RemoraGuild : IGuild
    {
        public ulong Id { get; }

        public string Name { get; }

        public IReadOnlyDictionary<ulong, IRole> Roles => m_roles;

        public IRole? BotRole => m_roles.Values.FirstOrDefault(r => r.IsThisBot);

        public IRole EveryoneRole { get; }

        public IReadOnlyDictionary<ulong, IMember> Members => m_members;

        public IReadOnlyCollection<IChannel> Channels => m_channels.Values.ToArray();

        public IReadOnlyCollection<IChannelCategory> ChannelCategories => m_categories.Values.ToArray();

        private readonly Dictionary<ulong, IRole> m_roles;
        private readonly Dictionary<ulong, IMember> m_members;
        private readonly Dictionary<ulong, IChannel> m_channels;
        private readonly Dictionary<ulong, IChannelCategory> m_categories;

        public RemoraGuild(ulong id, string name, IEnumerable<IRole>? roles = null, IEnumerable<IMember>? members = null, IEnumerable<IChannel>? channels = null, IEnumerable<IChannelCategory>? categories = null)
        {
            Id = id;
            Name = name;

            m_roles = roles != null ? roles.ToDictionary(r => r.Id, r => r) : new Dictionary<ulong, IRole>();
            m_members = members != null ? members.ToDictionary(m => m.Id, m => m) : new Dictionary<ulong, IMember>();
            m_channels = channels != null ? channels.ToDictionary(c => c.Id, c => c) : new Dictionary<ulong, IChannel>();
            m_categories = categories != null ? categories.ToDictionary(c => c.Id, c => c) : new Dictionary<ulong, IChannelCategory>();

            EveryoneRole = m_roles.Values.FirstOrDefault(r => r.Name.Equals("everyone", StringComparison.OrdinalIgnoreCase)) ?? new RemoraRole(0, "everyone");
            if (!m_roles.ContainsKey(EveryoneRole.Id))
            {
                m_roles[EveryoneRole.Id] = EveryoneRole;
            }
        }

        public IRole? GetRoleByName(string name)
        {
            return m_roles.Values.FirstOrDefault(r => r.Name.Equals(name, StringComparison.Ordinal));
        }

        public Task<IChannel?> CreateVoiceChannelAsync(string name, IChannelCategory? parent = null)
        {
            var channel = new RemoraChannel(GenerateIdFromName(name), name, true);
            m_channels[channel.Id] = channel;
            return Task.FromResult<IChannel?>(channel);
        }

        public Task<IChannel?> CreateTextChannelAsync(string name, IChannelCategory? parent = null)
        {
            var channel = new RemoraChannel(GenerateIdFromName(name), name, false);
            m_channels[channel.Id] = channel;
            return Task.FromResult<IChannel?>(channel);
        }

        public Task<IChannelCategory?> CreateCategoryAsync(string name)
        {
            var category = new RemoraChannelCategory(GenerateIdFromName(name), name);
            m_categories[category.Id] = category;
            return Task.FromResult<IChannelCategory?>(category);
        }

        public Task<IRole?> CreateRoleAsync(string name, Color color)
        {
            var role = new RemoraRole(GenerateIdFromName(name), name);
            m_roles[role.Id] = role;
            return Task.FromResult<IRole?>(role);
        }

        public IChannel? GetChannel(ulong id)
        {
            return m_channels.GetValueOrDefault(id);
        }

        public IChannel? GetChannelByName(string name)
        {
            return m_channels.Values.FirstOrDefault(c => c.Name.Equals(name, StringComparison.Ordinal));
        }

        public IChannelCategory? GetChannelCategory(ulong id)
        {
            return m_categories.GetValueOrDefault(id);
        }

        public IChannelCategory? GetCategoryByName(string name)
        {
            return m_categories.Values.FirstOrDefault(c => c.Name.Equals(name, StringComparison.Ordinal));
        }

        private static ulong GenerateIdFromName(string name)
        {
            unchecked
            {
                ulong hash = 14695981039346656037UL;
                foreach (char c in name)
                {
                    hash ^= c;
                    hash *= 1099511628211UL;
                }
                return hash;
            }
        }
    }
}
