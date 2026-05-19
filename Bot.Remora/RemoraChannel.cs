using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;

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

        private readonly IDiscordRestChannelAPI? m_channelApi;
        private readonly Dictionary<ulong, (IBaseChannel.Permissions Allow, IBaseChannel.Permissions Deny)> m_memberOverwrites = new();
        private readonly Dictionary<ulong, (IBaseChannel.Permissions Allow, IBaseChannel.Permissions Deny)> m_roleOverwrites = new();

        public RemoraChannel(ulong id, string name, bool isVoice = false, int position = 0, IEnumerable<IMember>? users = null, IDiscordRestChannelAPI? channelApi = null)
        {
            Id = id;
            Name = name;
            IsVoice = isVoice;
            Position = position;
            m_users = users != null ? new List<IMember>(users) : new List<IMember>();
            m_channelApi = channelApi;
        }

        public async Task AddOverwriteAsync(IMember member, IBaseChannel.Permissions allow, IBaseChannel.Permissions deny = IBaseChannel.Permissions.None)
        {
            m_memberOverwrites[member.Id] = (allow, deny);
            if (m_channelApi is null)
            {
                return;
            }

            IResult result = await m_channelApi.EditChannelPermissionsAsync(
                new Snowflake(Id),
                new Snowflake(member.Id),
                new Optional<global::Remora.Discord.API.Abstractions.Objects.IDiscordPermissionSet?>(ToDiscordPermissionSet(allow)),
                new Optional<global::Remora.Discord.API.Abstractions.Objects.IDiscordPermissionSet?>(ToDiscordPermissionSet(deny)),
                new Optional<global::Remora.Discord.API.Abstractions.Objects.PermissionOverwriteType>(global::Remora.Discord.API.Abstractions.Objects.PermissionOverwriteType.Member),
                default,
                default).ConfigureAwait(false);
            EnsureSuccess(result, "Failed to add member permission overwrite.");
        }

        public async Task AddOverwriteAsync(IRole role, IBaseChannel.Permissions allow, IBaseChannel.Permissions deny = IBaseChannel.Permissions.None)
        {
            m_roleOverwrites[role.Id] = (allow, deny);
            if (m_channelApi is null)
            {
                return;
            }

            IResult result = await m_channelApi.EditChannelPermissionsAsync(
                new Snowflake(Id),
                new Snowflake(role.Id),
                new Optional<global::Remora.Discord.API.Abstractions.Objects.IDiscordPermissionSet?>(ToDiscordPermissionSet(allow)),
                new Optional<global::Remora.Discord.API.Abstractions.Objects.IDiscordPermissionSet?>(ToDiscordPermissionSet(deny)),
                new Optional<global::Remora.Discord.API.Abstractions.Objects.PermissionOverwriteType>(global::Remora.Discord.API.Abstractions.Objects.PermissionOverwriteType.Role),
                default,
                default).ConfigureAwait(false);
            EnsureSuccess(result, "Failed to add role permission overwrite.");
        }

        public async Task RemoveOverwriteAsync(IRole role)
        {
            m_roleOverwrites.Remove(role.Id);
            if (m_channelApi is null)
            {
                return;
            }

            IResult result = await m_channelApi.DeleteChannelPermissionAsync(new Snowflake(Id), new Snowflake(role.Id), default, default).ConfigureAwait(false);
            EnsureSuccess(result, "Failed to remove role permission overwrite.");
        }

        public async Task<IMessage> SendMessageAsync(string msg)
        {
            if (m_channelApi is null)
            {
                return new RemoraMessage(msg);
            }

            IResult<global::Remora.Discord.API.Abstractions.Objects.IMessage> result = await m_channelApi.CreateMessageAsync(
                new Snowflake(Id),
                new Optional<string>(msg),
                ct: default).ConfigureAwait(false);
            EnsureSuccess(result, "Failed to send channel message.");
            return new RemoraMessage(msg);
        }

        public async Task<IMessage> SendMessageAsync(IEmbed embed)
        {
            if (embed is not RemoraEmbed)
            {
                throw new InvalidOperationException("Expected an embed that works with Remora");
            }

            if (m_channelApi is null)
            {
                return new RemoraMessage();
            }

            IResult<global::Remora.Discord.API.Abstractions.Objects.IMessage> result = await m_channelApi.CreateMessageAsync(
                new Snowflake(Id),
                embeds: new Optional<IReadOnlyList<global::Remora.Discord.API.Abstractions.Objects.IEmbed>>(new[] { new Embed() }),
                ct: default).ConfigureAwait(false);
            EnsureSuccess(result, "Failed to send channel embed message.");
            return new RemoraMessage();
        }

        public async Task<IMessage> SendMessageAsync(IMessageBuilder builder)
        {
            if (builder is not RemoraMessageBuilder remoraBuilder)
            {
                throw new InvalidOperationException("Expected a message builder that works with Remora");
            }

            if (m_channelApi is null)
            {
                return new RemoraMessage(remoraBuilder.Content);
            }

            IResult<global::Remora.Discord.API.Abstractions.Objects.IMessage> result = await m_channelApi.CreateMessageAsync(
                new Snowflake(Id),
                new Optional<string>(remoraBuilder.Content),
                ct: default).ConfigureAwait(false);
            EnsureSuccess(result, "Failed to send builder channel message.");
            return new RemoraMessage(remoraBuilder.Content);
        }

        public async Task RestrictOverwriteToMembersAsync(IReadOnlyCollection<IMember> memberPool, IBaseChannel.Permissions permission, IEnumerable<IMember> allowedMembers)
        {
            HashSet<ulong> allowed = allowedMembers.Select(m => m.Id).ToHashSet();
            HashSet<ulong> pool = memberPool.Select(m => m.Id).ToHashSet();

            foreach (var memberId in pool)
            {
                if (allowed.Contains(memberId))
                {
                    m_memberOverwrites[memberId] = (permission, IBaseChannel.Permissions.None);
                    if (m_channelApi is not null)
                    {
                        IResult setResult = await m_channelApi.EditChannelPermissionsAsync(
                            new Snowflake(Id),
                            new Snowflake(memberId),
                            new Optional<global::Remora.Discord.API.Abstractions.Objects.IDiscordPermissionSet?>(ToDiscordPermissionSet(permission)),
                            new Optional<global::Remora.Discord.API.Abstractions.Objects.IDiscordPermissionSet?>(DiscordPermissionSet.Empty),
                            new Optional<global::Remora.Discord.API.Abstractions.Objects.PermissionOverwriteType>(global::Remora.Discord.API.Abstractions.Objects.PermissionOverwriteType.Member),
                            default,
                            default).ConfigureAwait(false);
                        EnsureSuccess(setResult, "Failed to set member permission overwrite.");
                    }
                }
                else
                {
                    m_memberOverwrites.Remove(memberId);
                    if (m_channelApi is not null)
                    {
                        IResult removeResult = await m_channelApi.DeleteChannelPermissionAsync(
                            new Snowflake(Id),
                            new Snowflake(memberId),
                            default,
                            default).ConfigureAwait(false);
                        EnsureSuccess(removeResult, "Failed to remove member permission overwrite.");
                    }
                }
            }
        }

        public async Task DeleteAsync(string? reason = null)
        {
            IsDeleted = true;
            if (m_channelApi is null)
            {
                return;
            }

            IResult result = await m_channelApi.DeleteChannelAsync(
                new Snowflake(Id),
                string.IsNullOrWhiteSpace(reason) ? default : new Optional<string>(reason),
                default).ConfigureAwait(false);
            EnsureSuccess(result, "Failed to delete channel.");
        }

        private static DiscordPermissionSet ToDiscordPermissionSet(IBaseChannel.Permissions permissions)
        {
            ulong rawPermissions = Convert.ToUInt64(permissions);
            return new DiscordPermissionSet(new BigInteger(rawPermissions));
        }

        private static void EnsureSuccess(IResult result, string message)
        {
            if (result.IsSuccess)
            {
                return;
            }

            throw new InvalidOperationException($"{message}: {result.Error}");
        }
    }
}
