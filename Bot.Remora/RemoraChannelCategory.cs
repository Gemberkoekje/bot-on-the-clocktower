using Bot.Api;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Remora
{
    public class RemoraChannelCategory : IChannelCategory
    {
        public ulong Id { get; }

        public IReadOnlyCollection<IChannel> Channels => m_channels;

        public string Name { get; }

        public bool IsDeleted { get; private set; }

        private readonly List<IChannel> m_channels;

        private readonly Dictionary<ulong, (IBaseChannel.Permissions Allow, IBaseChannel.Permissions Deny)> m_memberOverwrites = new();
        private readonly Dictionary<ulong, (IBaseChannel.Permissions Allow, IBaseChannel.Permissions Deny)> m_roleOverwrites = new();

        public RemoraChannelCategory(ulong id, string name, IEnumerable<IChannel>? channels = null)
        {
            Id = id;
            Name = name;
            m_channels = channels != null ? new List<IChannel>(channels) : new List<IChannel>();
        }

        public Task AddOverwriteAsync(IMember member, IBaseChannel.Permissions allow, IBaseChannel.Permissions deny = IBaseChannel.Permissions.None)
        {
            m_memberOverwrites[member.Id] = (allow, deny);
            return Task.CompletedTask;
        }

        public Task AddOverwriteAsync(IRole role, IBaseChannel.Permissions allow, IBaseChannel.Permissions deny = IBaseChannel.Permissions.None)
        {
            m_roleOverwrites[role.Id] = (allow, deny);
            return Task.CompletedTask;
        }

        public Task RemoveOverwriteAsync(IRole role)
        {
            m_roleOverwrites.Remove(role.Id);
            return Task.CompletedTask;
        }

        public IChannel? GetChannelByName(string name)
        {
            return m_channels.FirstOrDefault(c => c.Name == name);
        }

        public Task DeleteAsync(string? reason = null)
        {
            IsDeleted = true;
            return Task.CompletedTask;
        }
    }
}
