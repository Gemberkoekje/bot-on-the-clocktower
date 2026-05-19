using Bot.Api;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Results;

namespace Bot.Remora
{
    public class RemoraGuild : IGuild
    {
        public ulong Id { get; }

        public string Name { get; }

        public IReadOnlyDictionary<ulong, IRole> Roles
        {
            get
            {
                EnsureRolesLoaded();
                return m_roles;
            }
        }

        public IRole? BotRole
        {
            get
            {
                EnsureRolesLoaded();
                return m_roles.Values.FirstOrDefault(r => r.IsThisBot);
            }
        }

        public IRole EveryoneRole
        {
            get
            {
                EnsureRolesLoaded();
                return m_everyoneRole!;
            }
        }

        public IReadOnlyDictionary<ulong, IMember> Members
        {
            get
            {
                EnsureMembersLoaded();
                return m_members;
            }
        }

        public IReadOnlyCollection<IChannel> Channels
        {
            get
            {
                EnsureChannelsLoaded();
                return m_channels.Values.ToArray();
            }
        }

        public IReadOnlyCollection<IChannelCategory> ChannelCategories
        {
            get
            {
                EnsureChannelsLoaded();
                return m_categories.Values.ToArray();
            }
        }

        private const int GuildMemberPageSize = 1000;
        private const string WriteNotSupportedMessage = "RemoraGuild write operations are not implemented yet (planned for later runtime phases).";
        private readonly IDiscordRestGuildAPI? m_guildApi;
        private readonly object m_sync = new();
        private Dictionary<ulong, IRole> m_roles;
        private Dictionary<ulong, IMember> m_members;
        private Dictionary<ulong, IChannel> m_channels;
        private Dictionary<ulong, IChannelCategory> m_categories;
        private bool m_rolesLoaded;
        private bool m_membersLoaded;
        private bool m_channelsLoaded;
        private IRole? m_everyoneRole;

        public RemoraGuild(ulong id, string name, IEnumerable<IRole>? roles = null, IEnumerable<IMember>? members = null, IEnumerable<IChannel>? channels = null, IEnumerable<IChannelCategory>? categories = null)
        {
            Id = id;
            Name = name;
            m_guildApi = null;

            m_roles = roles != null ? roles.ToDictionary(r => r.Id, r => r) : new Dictionary<ulong, IRole>();
            m_members = members != null ? members.ToDictionary(m => m.Id, m => m) : new Dictionary<ulong, IMember>();
            m_channels = channels != null ? channels.ToDictionary(c => c.Id, c => c) : new Dictionary<ulong, IChannel>();
            m_categories = categories != null ? categories.ToDictionary(c => c.Id, c => c) : new Dictionary<ulong, IChannelCategory>();
            m_rolesLoaded = true;
            m_membersLoaded = true;
            m_channelsLoaded = true;
            EnsureEveryoneRolePresent();
        }

        public RemoraGuild(ulong id, string name, IDiscordRestGuildAPI guildApi)
        {
            Id = id;
            Name = name;
            m_guildApi = guildApi ?? throw new ArgumentNullException(nameof(guildApi));
            m_roles = new Dictionary<ulong, IRole>();
            m_members = new Dictionary<ulong, IMember>();
            m_channels = new Dictionary<ulong, IChannel>();
            m_categories = new Dictionary<ulong, IChannelCategory>();
        }

        public IRole? GetRoleByName(string name)
        {
            EnsureRolesLoaded();
            return m_roles.Values.FirstOrDefault(r => r.Name.Equals(name, StringComparison.Ordinal));
        }

        public Task<IChannel?> CreateVoiceChannelAsync(string name, IChannelCategory? parent = null)
        {
            throw new NotSupportedException(WriteNotSupportedMessage);
        }

        public Task<IChannel?> CreateTextChannelAsync(string name, IChannelCategory? parent = null)
        {
            throw new NotSupportedException(WriteNotSupportedMessage);
        }

        public Task<IChannelCategory?> CreateCategoryAsync(string name)
        {
            throw new NotSupportedException(WriteNotSupportedMessage);
        }

        public Task<IRole?> CreateRoleAsync(string name, Color color)
        {
            throw new NotSupportedException(WriteNotSupportedMessage);
        }

        public IChannel? GetChannel(ulong id)
        {
            EnsureChannelsLoaded();
            return m_channels.GetValueOrDefault(id);
        }

        public IChannel? GetChannelByName(string name)
        {
            EnsureChannelsLoaded();
            return m_channels.Values.FirstOrDefault(c => c.Name.Equals(name, StringComparison.Ordinal));
        }

        public IChannelCategory? GetChannelCategory(ulong id)
        {
            EnsureChannelsLoaded();
            return m_categories.GetValueOrDefault(id);
        }

        public IChannelCategory? GetCategoryByName(string name)
        {
            EnsureChannelsLoaded();
            return m_categories.Values.FirstOrDefault(c => c.Name.Equals(name, StringComparison.Ordinal));
        }

        private void EnsureRolesLoaded()
        {
            if (m_rolesLoaded)
            {
                return;
            }

            lock (m_sync)
            {
                if (m_rolesLoaded)
                {
                    return;
                }

                if (m_guildApi is null)
                {
                    m_rolesLoaded = true;
                    EnsureEveryoneRolePresent();
                    return;
                }

                m_roles = LoadRolesAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                m_rolesLoaded = true;
                EnsureEveryoneRolePresent();
            }
        }

        private void EnsureMembersLoaded()
        {
            if (m_membersLoaded)
            {
                return;
            }

            lock (m_sync)
            {
                if (m_membersLoaded)
                {
                    return;
                }

                if (m_guildApi is null)
                {
                    m_membersLoaded = true;
                    return;
                }

                EnsureRolesLoaded();
                m_members = LoadMembersAsync(m_roles).ConfigureAwait(false).GetAwaiter().GetResult();
                m_membersLoaded = true;
            }
        }

        private void EnsureChannelsLoaded()
        {
            if (m_channelsLoaded)
            {
                return;
            }

            lock (m_sync)
            {
                if (m_channelsLoaded)
                {
                    return;
                }

                if (m_guildApi is null)
                {
                    m_channelsLoaded = true;
                    return;
                }

                (m_channels, m_categories) = LoadChannelsAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                m_channelsLoaded = true;
            }
        }

        private async Task<Dictionary<ulong, IRole>> LoadRolesAsync()
        {
            Result<IReadOnlyList<global::Remora.Discord.API.Abstractions.Objects.IRole>> result = await m_guildApi!.GetGuildRolesAsync(new Snowflake(Id), CancellationToken.None).ConfigureAwait(false);
            EnsureSuccess(result, "Failed to fetch guild roles.");

            Dictionary<ulong, IRole> roles = new();
            foreach (global::Remora.Discord.API.Abstractions.Objects.IRole discordRole in result.Entity)
            {
                bool isBotRole = discordRole.Tags.HasValue && discordRole.Tags.Value.BotID.HasValue;
                RemoraRole role = new(discordRole.ID.Value, discordRole.Name, isBotRole);
                roles[role.Id] = role;
            }

            return roles;
        }

        private async Task<(Dictionary<ulong, IChannel> channels, Dictionary<ulong, IChannelCategory> categories)> LoadChannelsAsync()
        {
            Result<IReadOnlyList<global::Remora.Discord.API.Abstractions.Objects.IChannel>> result = await m_guildApi!.GetGuildChannelsAsync(new Snowflake(Id), CancellationToken.None).ConfigureAwait(false);
            EnsureSuccess(result, "Failed to fetch guild channels.");

            Dictionary<ulong, IChannel> channels = new();
            Dictionary<ulong, string> categoryNames = new();
            Dictionary<ulong, List<IChannel>> categoryChildren = new();

            foreach (global::Remora.Discord.API.Abstractions.Objects.IChannel discordChannel in result.Entity)
            {
                ulong channelId = discordChannel.ID.Value;
                string channelName = discordChannel.Name.HasValue && !string.IsNullOrWhiteSpace(discordChannel.Name.Value)
                    ? discordChannel.Name.Value
                    : $"channel-{channelId}";

                if (discordChannel.Type == global::Remora.Discord.API.Abstractions.Objects.ChannelType.GuildCategory)
                {
                    categoryNames[channelId] = channelName;
                    continue;
                }

                bool isVoice = discordChannel.Type == global::Remora.Discord.API.Abstractions.Objects.ChannelType.GuildVoice
                    || discordChannel.Type == global::Remora.Discord.API.Abstractions.Objects.ChannelType.GuildStageVoice;
                int position = discordChannel.Position.HasValue ? discordChannel.Position.Value : 0;
                RemoraChannel channel = new(channelId, channelName, isVoice, position);
                channels[channelId] = channel;

                if (discordChannel.ParentID.HasValue && discordChannel.ParentID.Value.HasValue)
                {
                    ulong parentId = discordChannel.ParentID.Value.Value.Value;
                    if (!categoryChildren.TryGetValue(parentId, out var children))
                    {
                        children = new List<IChannel>();
                        categoryChildren[parentId] = children;
                    }

                    children.Add(channel);
                }
            }

            Dictionary<ulong, IChannelCategory> categories = categoryNames.ToDictionary(
                entry => entry.Key,
                entry =>
                {
                    IReadOnlyCollection<IChannel> children = categoryChildren.TryGetValue(entry.Key, out var list)
                        ? list
                        : Array.Empty<IChannel>();
                    return (IChannelCategory)new RemoraChannelCategory(entry.Key, entry.Value, children);
                });

            return (channels, categories);
        }

        private async Task<Dictionary<ulong, IMember>> LoadMembersAsync(IReadOnlyDictionary<ulong, IRole> roles)
        {
            Dictionary<ulong, IMember> members = new();
            Optional<Snowflake> after = default;

            while (true)
            {
                Result<IReadOnlyList<global::Remora.Discord.API.Abstractions.Objects.IGuildMember>> result = await m_guildApi!.ListGuildMembersAsync(
                    new Snowflake(Id),
                    new Optional<int>(GuildMemberPageSize),
                    after,
                    CancellationToken.None).ConfigureAwait(false);
                EnsureSuccess(result, "Failed to fetch guild members.");

                IReadOnlyList<global::Remora.Discord.API.Abstractions.Objects.IGuildMember> page = result.Entity;
                if (page.Count == 0)
                {
                    break;
                }

                foreach (global::Remora.Discord.API.Abstractions.Objects.IGuildMember guildMember in page)
                {
                    if (!guildMember.User.HasValue)
                    {
                        continue;
                    }

                    global::Remora.Discord.API.Abstractions.Objects.IUser user = guildMember.User.Value;
                    ulong memberId = user.ID.Value;
                    string displayName = guildMember.Nickname.HasValue && !string.IsNullOrWhiteSpace(guildMember.Nickname.Value)
                        ? guildMember.Nickname.Value
                        : user.Username ?? $"member-{memberId}";
                    bool isBot = user.IsBot.HasValue && user.IsBot.Value;
                    IReadOnlyCollection<IRole> memberRoles = guildMember.Roles
                        .Select(roleId => roles.GetValueOrDefault(roleId.Value))
                        .Where(role => role is not null)
                        .Cast<IRole>()
                        .ToArray();

                    members[memberId] = new RemoraMember(memberId, displayName, isBot, memberRoles);
                }

                if (page.Count < GuildMemberPageSize)
                {
                    break;
                }

                global::Remora.Discord.API.Abstractions.Objects.IGuildMember? lastMember = page.LastOrDefault(member => member.User.HasValue);
                if (lastMember is null || !lastMember.User.HasValue)
                {
                    break;
                }

                after = new Optional<Snowflake>(lastMember.User.Value.ID);
            }

            return members;
        }

        private void EnsureEveryoneRolePresent()
        {
            m_everyoneRole = m_roles.Values.FirstOrDefault(IsEveryoneRole) ?? new RemoraRole(Id, "everyone");
            if (!m_roles.ContainsKey(m_everyoneRole.Id))
            {
                m_roles[m_everyoneRole.Id] = m_everyoneRole;
            }
        }

        private bool IsEveryoneRole(IRole role)
        {
            return role.Id == Id
                || role.Name.Equals("everyone", StringComparison.OrdinalIgnoreCase)
                || role.Name.Equals("@everyone", StringComparison.OrdinalIgnoreCase);
        }

        private static void EnsureSuccess(IResult result, string message)
        {
            if (result.IsSuccess)
            {
                return;
            }

            throw new InvalidOperationException($"{message} {result.Error}");
        }
    }
}
