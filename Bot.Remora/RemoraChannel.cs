using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Remora
{
    public class RemoraChannel : IChannel
    {
        public ulong Id { get; }

        public IReadOnlyCollection<IMember> Users => m_users;

        public int Position { get; }

        public bool IsVoice { get; }

        public bool IsText => !IsVoice;

        public string Name { get; }

        private readonly List<IMember> m_users;

        public bool IsDeleted { get; private set; }

        private readonly Dictionary<ulong, (IBaseChannel.Permissions Allow, IBaseChannel.Permissions Deny)> m_memberOverwrites = new();
        private readonly Dictionary<ulong, (IBaseChannel.Permissions Allow, IBaseChannel.Permissions Deny)> m_roleOverwrites = new();

        public RemoraChannel(ulong id, string name, bool isVoice = false, int position = 0, IEnumerable<IMember>? users = null)
        {
            Id = id;
            Name = name;
            IsVoice = isVoice;
            Position = position;
            m_users = users != null ? new List<IMember>(users) : new List<IMember>();
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

        public Task<IMessage> SendMessageAsync(string msg)
        {
            return Task.FromResult<IMessage>(new RemoraMessage(msg));
        }

        public Task<IMessage> SendMessageAsync(IEmbed embed)
        {
            if (embed is not RemoraEmbed)
            {
                throw new InvalidOperationException("Expected an embed that works with Remora");
            }

            return Task.FromResult<IMessage>(new RemoraMessage());
        }

        public Task<IMessage> SendMessageAsync(IMessageBuilder builder)
        {
            if (builder is not RemoraMessageBuilder remoraBuilder)
            {
                throw new InvalidOperationException("Expected a message builder that works with Remora");
            }

            return Task.FromResult<IMessage>(new RemoraMessage(remoraBuilder.Content));
        }

        public Task RestrictOverwriteToMembersAsync(IReadOnlyCollection<IMember> memberPool, IBaseChannel.Permissions permission, IEnumerable<IMember> allowedMembers)
        {
            HashSet<ulong> allowed = allowedMembers.Select(m => m.Id).ToHashSet();
            HashSet<ulong> pool = memberPool.Select(m => m.Id).ToHashSet();

            foreach (var memberId in pool)
            {
                if (allowed.Contains(memberId))
                {
                    m_memberOverwrites[memberId] = (permission, IBaseChannel.Permissions.None);
                }
                else
                {
                    m_memberOverwrites.Remove(memberId);
                }
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string? reason = null)
        {
            IsDeleted = true;
            return Task.CompletedTask;
        }
    }
}
