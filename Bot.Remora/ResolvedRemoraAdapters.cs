using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api = Bot.Api;
using Discord = Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace Bot.Remora
{
    internal static class ResolvedAdapterExceptions
    {
        public const string MutationNotSupportedMessage = "Resolved interaction entities are read-only in phase 2.";
    }

    internal sealed class ResolvedMemberAdapter : Api.IMember
    {
        public ResolvedMemberAdapter(
            Discord.IUser user,
            Optional<string?> nickname,
            Optional<IReadOnlyList<Snowflake>> roleIds,
            IReadOnlyDictionary<Snowflake, Discord.IRole> resolvedRoles)
        {
            Id = user.ID.Value;
            DisplayName = ResolveDisplayName(user, nickname);
            IsBot = user.IsBot.HasValue && user.IsBot.Value;
            Roles = ResolveRoles(roleIds, resolvedRoles);
        }

        public string DisplayName { get; }

        public bool IsBot { get; }

        public ulong Id { get; }

        public IReadOnlyCollection<Api.IRole> Roles { get; }

        public Task MoveToChannelAsync(Api.IChannel c)
            => throw new NotSupportedException(ResolvedAdapterExceptions.MutationNotSupportedMessage);

        public Task GrantRoleAsync(Api.IRole role)
            => throw new NotSupportedException(ResolvedAdapterExceptions.MutationNotSupportedMessage);

        public Task RevokeRoleAsync(Api.IRole role)
            => throw new NotSupportedException(ResolvedAdapterExceptions.MutationNotSupportedMessage);

        public Task<Api.IMessage> SendMessageAsync(string content)
            => throw new NotSupportedException(ResolvedAdapterExceptions.MutationNotSupportedMessage);

        public Task SetDisplayName(string name)
            => throw new NotSupportedException(ResolvedAdapterExceptions.MutationNotSupportedMessage);

        private static string ResolveDisplayName(Discord.IUser user, Optional<string?> nickname)
        {
            if (nickname.HasValue && !string.IsNullOrWhiteSpace(nickname.Value))
            {
                return nickname.Value;
            }

            return user.Username ?? "unknown-member";
        }

        private static IReadOnlyCollection<Api.IRole> ResolveRoles(
            Optional<IReadOnlyList<Snowflake>> roleIds,
            IReadOnlyDictionary<Snowflake, Discord.IRole> resolvedRoles)
        {
            if (!roleIds.HasValue)
            {
                return Array.Empty<Api.IRole>();
            }

            return roleIds.Value
                .Where(roleId => resolvedRoles.ContainsKey(roleId))
                .Select(roleId => (Api.IRole)new ResolvedRoleAdapter(resolvedRoles[roleId]))
                .ToArray();
        }
    }

    internal sealed class ResolvedRoleAdapter : Api.IRole
    {
        public ResolvedRoleAdapter(Discord.IRole role)
        {
            Id = role.ID.Value;
            Name = role.Name;
            Mention = $"<@&{Id}>";
        }

        public string Name { get; }

        public string Mention { get; }

        public ulong Id { get; }

        public bool IsThisBot => false;

        public Task DeleteAsync()
            => throw new NotSupportedException(ResolvedAdapterExceptions.MutationNotSupportedMessage);
    }

    internal sealed class ResolvedChannelAdapter : Api.IChannel
    {
        public ResolvedChannelAdapter(Discord.IPartialChannel channel)
        {
            Id = channel.ID.HasValue ? channel.ID.Value.Value : 0UL;
            Name = channel.Name.HasValue && !string.IsNullOrWhiteSpace(channel.Name.Value)
                ? channel.Name.Value
                : $"channel-{Id}";
            Position = channel.Position.HasValue ? channel.Position.Value : 0;
            IsVoice = channel.Type.HasValue
                && (channel.Type.Value == Discord.ChannelType.GuildVoice || channel.Type.Value == Discord.ChannelType.GuildStageVoice);
            Users = Array.Empty<Api.IMember>();
        }

        public ulong Id { get; }

        public IReadOnlyCollection<Api.IMember> Users { get; }

        public int Position { get; }

        public bool IsVoice { get; }

        public bool IsText => !IsVoice;

        public string Name { get; }

        public Task AddOverwriteAsync(Api.IMember member, Api.IBaseChannel.Permissions allow, Api.IBaseChannel.Permissions deny = Api.IBaseChannel.Permissions.None)
            => throw new NotSupportedException(ResolvedAdapterExceptions.MutationNotSupportedMessage);

        public Task AddOverwriteAsync(Api.IRole role, Api.IBaseChannel.Permissions allow, Api.IBaseChannel.Permissions deny = Api.IBaseChannel.Permissions.None)
            => throw new NotSupportedException(ResolvedAdapterExceptions.MutationNotSupportedMessage);

        public Task RemoveOverwriteAsync(Api.IRole role)
            => throw new NotSupportedException(ResolvedAdapterExceptions.MutationNotSupportedMessage);

        public Task<Api.IMessage> SendMessageAsync(string msg)
            => throw new NotSupportedException(ResolvedAdapterExceptions.MutationNotSupportedMessage);

        public Task<Api.IMessage> SendMessageAsync(Api.IEmbed embed)
            => throw new NotSupportedException(ResolvedAdapterExceptions.MutationNotSupportedMessage);

        public Task<Api.IMessage> SendMessageAsync(Api.IMessageBuilder builder)
            => throw new NotSupportedException(ResolvedAdapterExceptions.MutationNotSupportedMessage);

        public Task RestrictOverwriteToMembersAsync(IReadOnlyCollection<Api.IMember> memberPool, Api.IBaseChannel.Permissions permission, IEnumerable<Api.IMember> allowedMembers)
            => throw new NotSupportedException(ResolvedAdapterExceptions.MutationNotSupportedMessage);

        public Task DeleteAsync(string? reason = null)
            => throw new NotSupportedException(ResolvedAdapterExceptions.MutationNotSupportedMessage);
    }

    internal sealed class ResolvedChannelCategoryAdapter : Api.IChannelCategory
    {
        public ResolvedChannelCategoryAdapter(Discord.IPartialChannel channel)
        {
            Id = channel.ID.HasValue ? channel.ID.Value.Value : 0UL;
            Name = channel.Name.HasValue && !string.IsNullOrWhiteSpace(channel.Name.Value)
                ? channel.Name.Value
                : $"category-{Id}";
            Channels = Array.Empty<Api.IChannel>();
        }

        public ulong Id { get; }

        public IReadOnlyCollection<Api.IChannel> Channels { get; }

        public string Name { get; }

        public Task AddOverwriteAsync(Api.IMember member, Api.IBaseChannel.Permissions allow, Api.IBaseChannel.Permissions deny = Api.IBaseChannel.Permissions.None)
            => throw new NotSupportedException(ResolvedAdapterExceptions.MutationNotSupportedMessage);

        public Task AddOverwriteAsync(Api.IRole role, Api.IBaseChannel.Permissions allow, Api.IBaseChannel.Permissions deny = Api.IBaseChannel.Permissions.None)
            => throw new NotSupportedException(ResolvedAdapterExceptions.MutationNotSupportedMessage);

        public Task RemoveOverwriteAsync(Api.IRole role)
            => throw new NotSupportedException(ResolvedAdapterExceptions.MutationNotSupportedMessage);

        public Api.IChannel? GetChannelByName(string name)
            => null;

        public Task DeleteAsync(string? reason = null)
            => throw new NotSupportedException(ResolvedAdapterExceptions.MutationNotSupportedMessage);
    }
}
