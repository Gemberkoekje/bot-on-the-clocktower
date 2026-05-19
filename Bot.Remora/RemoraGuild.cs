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
    public class RemoraGuild : Bot.Api.IGuild
    {
        public ulong Id { get; }

        public string Name { get; }

        public IReadOnlyDictionary<ulong, Bot.Api.IRole> Roles
        {
            get
            {
                EnsureRolesLoaded();
                return m_roles;
            }
        }

        public Bot.Api.IRole? BotRole
        {
            get
            {
                EnsureRolesLoaded();
                return m_roles.Values.FirstOrDefault(r => r.IsThisBot);
            }
        }

        public Bot.Api.IRole EveryoneRole
        {
            get
            {
                EnsureRolesLoaded();
                return m_everyoneRole!;
            }
        }

        public IReadOnlyDictionary<ulong, Bot.Api.IMember> Members
        {
            get
            {
                EnsureMembersLoaded();
                return m_members;
            }
        }

        public IReadOnlyCollection<Bot.Api.IChannel> Channels
        {
            get
            {
                EnsureChannelsLoaded();
                return m_channels.Values.ToArray();
            }
        }

        public IReadOnlyCollection<Bot.Api.IChannelCategory> ChannelCategories
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
        private readonly IDiscordRestChannelAPI? m_channelApi;
        private readonly IDiscordRestUserAPI? m_userApi;
        private readonly object m_sync = new();
        private Dictionary<ulong, Bot.Api.IRole> m_roles;
        private Dictionary<ulong, Bot.Api.IMember> m_members;
        private Dictionary<ulong, Bot.Api.IChannel> m_channels;
        private Dictionary<ulong, Bot.Api.IChannelCategory> m_categories;
        private bool m_rolesLoaded;
        private bool m_membersLoaded;
        private bool m_channelsLoaded;
        private Bot.Api.IRole? m_everyoneRole;

        public RemoraGuild(ulong id, string name, IEnumerable<Bot.Api.IRole>? roles = null, IEnumerable<Bot.Api.IMember>? members = null, IEnumerable<Bot.Api.IChannel>? channels = null, IEnumerable<Bot.Api.IChannelCategory>? categories = null)
        {
            Id = id;
            Name = name;
            m_guildApi = null;
            m_channelApi = null;
            m_userApi = null;

            m_roles = roles != null ? roles.ToDictionary(r => r.Id, r => r) : new Dictionary<ulong, Bot.Api.IRole>();
            m_members = members != null ? members.ToDictionary(m => m.Id, m => m) : new Dictionary<ulong, Bot.Api.IMember>();
            m_channels = channels != null ? channels.ToDictionary(c => c.Id, c => c) : new Dictionary<ulong, Bot.Api.IChannel>();
            m_categories = categories != null ? categories.ToDictionary(c => c.Id, c => c) : new Dictionary<ulong, Bot.Api.IChannelCategory>();
            m_rolesLoaded = true;
            m_membersLoaded = true;
            m_channelsLoaded = true;
            EnsureEveryoneRolePresent();
        }

        public RemoraGuild(ulong id, string name, IDiscordRestGuildAPI guildApi, IDiscordRestChannelAPI? channelApi = null, IDiscordRestUserAPI? userApi = null)
        {
            Id = id;
            Name = name;
            m_guildApi = guildApi ?? throw new ArgumentNullException(nameof(guildApi));
            m_channelApi = channelApi;
            m_userApi = userApi;
            m_roles = new Dictionary<ulong, Bot.Api.IRole>();
            m_members = new Dictionary<ulong, Bot.Api.IMember>();
            m_channels = new Dictionary<ulong, Bot.Api.IChannel>();
            m_categories = new Dictionary<ulong, Bot.Api.IChannelCategory>();
        }

        public Bot.Api.IRole? GetRoleByName(string name)
        {
            EnsureRolesLoaded();
            return m_roles.Values.FirstOrDefault(r => r.Name.Equals(name, StringComparison.Ordinal));
        }

        public async Task<Bot.Api.IChannel?> CreateVoiceChannelAsync(string name, Bot.Api.IChannelCategory? parent = null)
        {
            if (m_guildApi is null)
            {
                throw new NotSupportedException(WriteNotSupportedMessage);
            }

            Optional<Snowflake?> parentId = parent is null
                ? default
                : new Optional<Snowflake?>(new Snowflake(parent.Id));

            Result<global::Remora.Discord.API.Abstractions.Objects.IChannel> result = await m_guildApi.CreateGuildChannelAsync(
                new Snowflake(Id),
                name,
                new Optional<global::Remora.Discord.API.Abstractions.Objects.ChannelType?>(global::Remora.Discord.API.Abstractions.Objects.ChannelType.GuildVoice),
                parentID: parentId,
                ct: CancellationToken.None).ConfigureAwait(false);
            EnsureSuccess(result, "Failed to create voice channel.");

            global::Remora.Discord.API.Abstractions.Objects.IChannel created = result.Entity;
            ulong channelId = created.ID.Value;
            int position = created.Position.HasValue ? created.Position.Value : 0;
            RemoraChannel channel = new(channelId, name, isVoice: true, position: position, channelApi: m_channelApi, parentCategory: parent as RemoraChannelCategory);

            m_channels[channelId] = channel;
            if (parent is not null && m_categories.TryGetValue(parent.Id, out Bot.Api.IChannelCategory? existingCategory) && existingCategory is RemoraChannelCategory category)
            {
                category.AddChannel(channel);
            }

            return channel;
        }

        public async Task<Bot.Api.IChannel?> CreateTextChannelAsync(string name, Bot.Api.IChannelCategory? parent = null)
        {
            if (m_guildApi is null)
            {
                throw new NotSupportedException(WriteNotSupportedMessage);
            }

            Optional<Snowflake?> parentId = parent is null
                ? default
                : new Optional<Snowflake?>(new Snowflake(parent.Id));

            Result<global::Remora.Discord.API.Abstractions.Objects.IChannel> result = await m_guildApi.CreateGuildChannelAsync(
                new Snowflake(Id),
                name,
                new Optional<global::Remora.Discord.API.Abstractions.Objects.ChannelType?>(global::Remora.Discord.API.Abstractions.Objects.ChannelType.GuildText),
                parentID: parentId,
                ct: CancellationToken.None).ConfigureAwait(false);
            EnsureSuccess(result, "Failed to create text channel.");

            global::Remora.Discord.API.Abstractions.Objects.IChannel created = result.Entity;
            ulong channelId = created.ID.Value;
            int position = created.Position.HasValue ? created.Position.Value : 0;
            RemoraChannel channel = new(channelId, name, isVoice: false, position: position, channelApi: m_channelApi, parentCategory: parent as RemoraChannelCategory);

            m_channels[channelId] = channel;
            if (parent is not null && m_categories.TryGetValue(parent.Id, out Bot.Api.IChannelCategory? existingCategory) && existingCategory is RemoraChannelCategory category)
            {
                category.AddChannel(channel);
            }

            return channel;
        }

        public async Task<Bot.Api.IChannelCategory?> CreateCategoryAsync(string name)
        {
            if (m_guildApi is null)
            {
                throw new NotSupportedException(WriteNotSupportedMessage);
            }

            Result<global::Remora.Discord.API.Abstractions.Objects.IChannel> result = await m_guildApi.CreateGuildChannelAsync(
                new Snowflake(Id),
                name,
                new Optional<global::Remora.Discord.API.Abstractions.Objects.ChannelType?>(global::Remora.Discord.API.Abstractions.Objects.ChannelType.GuildCategory),
                ct: CancellationToken.None).ConfigureAwait(false);
            EnsureSuccess(result, "Failed to create category.");

            global::Remora.Discord.API.Abstractions.Objects.IChannel created = result.Entity;
            ulong categoryId = created.ID.Value;
            RemoraChannelCategory category = new(categoryId, name, channelApi: m_channelApi);
            m_categories[categoryId] = category;

            return category;
        }

        public async Task<Bot.Api.IRole?> CreateRoleAsync(string name, Color color)
        {
            if (m_guildApi is null)
            {
                throw new NotSupportedException(WriteNotSupportedMessage);
            }

            Result<global::Remora.Discord.API.Abstractions.Objects.IRole> result = await m_guildApi.CreateGuildRoleAsync(
                new Snowflake(Id),
                new Optional<string>(name),
                default,
                new Optional<Color>(color),
                ct: CancellationToken.None).ConfigureAwait(false);
            EnsureSuccess(result, "Failed to create role.");

            global::Remora.Discord.API.Abstractions.Objects.IRole created = result.Entity;
            RemoraRole role = new(created.ID.Value, created.Name, isThisBot: created.Tags.HasValue && created.Tags.Value.BotID.HasValue);
            m_roles[role.Id] = role;
            EnsureEveryoneRolePresent();

            return role;
        }

        public Bot.Api.IChannel? GetChannel(ulong id)
        {
            EnsureChannelsLoaded();
            return m_channels.GetValueOrDefault(id);
        }

        public Bot.Api.IChannel? GetChannelByName(string name)
        {
            EnsureChannelsLoaded();
            return m_channels.Values.FirstOrDefault(c => c.Name.Equals(name, StringComparison.Ordinal));
        }

        public Bot.Api.IChannelCategory? GetChannelCategory(ulong id)
        {
            EnsureChannelsLoaded();
            return m_categories.GetValueOrDefault(id);
        }

        public Bot.Api.IChannelCategory? GetCategoryByName(string name)
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
                // Ensure all loaded categories have the channel API reference for REST operations
                if (m_channelApi is not null)
                {
                    foreach (var category in m_categories.Values.OfType<RemoraChannelCategory>())
                    {
                        category.SetChannelApi(m_channelApi);
                    }
                }
                m_channelsLoaded = true;
            }
        }

        private async Task<Dictionary<ulong, Bot.Api.IRole>> LoadRolesAsync()
        {
            // Determine this bot's own user ID so we can correctly identify our role
            ulong? thisBotUserId = null;
            if (m_userApi is not null)
            {
                Result<global::Remora.Discord.API.Abstractions.Objects.IUser> currentUserResult = await m_userApi.GetCurrentUserAsync(CancellationToken.None).ConfigureAwait(false);
                if (currentUserResult.IsSuccess)
                {
                    thisBotUserId = currentUserResult.Entity.ID.Value;
                    Console.WriteLine($"RemoraGuild: Current bot user ID resolved. BotUserId={thisBotUserId}");
                }
                else
                {
                    Console.WriteLine($"RemoraGuild: Failed to resolve current bot user ID. Error={currentUserResult.Error?.Message}");
                }
            }

            Result<IReadOnlyList<global::Remora.Discord.API.Abstractions.Objects.IRole>> result = await m_guildApi!.GetGuildRolesAsync(new Snowflake(Id), CancellationToken.None).ConfigureAwait(false);
            EnsureSuccess(result, "Failed to fetch guild roles.");

            Dictionary<ulong, Bot.Api.IRole> roles = new();
            foreach (global::Remora.Discord.API.Abstractions.Objects.IRole discordRole in result.Entity)
            {
                bool isBotRole = discordRole.Tags.HasValue
                    && discordRole.Tags.Value.BotID.HasValue
                    && (thisBotUserId is null || discordRole.Tags.Value.BotID.Value.Value == thisBotUserId.Value);
                RemoraRole role = new(discordRole.ID.Value, discordRole.Name, isBotRole);
                roles[role.Id] = role;
                if (isBotRole)
                    Console.WriteLine($"RemoraGuild: Identified this bot's role. RoleName={role.Name}, RoleId={role.Id}");
            }

            return roles;
        }

        private async Task<(Dictionary<ulong, Bot.Api.IChannel> channels, Dictionary<ulong, Bot.Api.IChannelCategory> categories)> LoadChannelsAsync()
        {
            Result<IReadOnlyList<global::Remora.Discord.API.Abstractions.Objects.IChannel>> result = await m_guildApi!.GetGuildChannelsAsync(new Snowflake(Id), CancellationToken.None).ConfigureAwait(false);
            EnsureSuccess(result, "Failed to fetch guild channels.");

            Dictionary<ulong, (string Name, bool IsVoice, int Position, ulong? ParentId)> pendingChannels = new();
            Dictionary<ulong, string> categoryNames = new();

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
                ulong? parentId = discordChannel.ParentID.HasValue && discordChannel.ParentID.Value.HasValue
                    ? discordChannel.ParentID.Value.Value.Value
                    : null;
                pendingChannels[channelId] = (channelName, isVoice, position, parentId);
            }

            Dictionary<ulong, Bot.Api.IChannelCategory> categories = categoryNames.ToDictionary(
                entry => entry.Key,
                entry => (Bot.Api.IChannelCategory)new RemoraChannelCategory(entry.Key, entry.Value, channelApi: m_channelApi));

            Dictionary<ulong, Bot.Api.IChannel> channels = new();
            foreach ((ulong channelId, (string Name, bool IsVoice, int Position, ulong? ParentId) channelInfo) in pendingChannels)
            {
                RemoraChannelCategory? parentCategory = channelInfo.ParentId.HasValue && categories.TryGetValue(channelInfo.ParentId.Value, out Bot.Api.IChannelCategory? category)
                    ? category as RemoraChannelCategory
                    : null;

                RemoraChannel channel = new(channelId, channelInfo.Name, channelInfo.IsVoice, channelInfo.Position, channelApi: m_channelApi, parentCategory: parentCategory);
                channels[channelId] = channel;

                if (parentCategory is not null)
                {
                    parentCategory.AddChannel(channel);
                }
            }

            return (channels, categories);
        }

        private async Task<Dictionary<ulong, Bot.Api.IMember>> LoadMembersAsync(IReadOnlyDictionary<ulong, Bot.Api.IRole> roles)
        {
            Dictionary<ulong, Bot.Api.IMember> members = new();
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
                    IReadOnlyCollection<Bot.Api.IRole> memberRoles = guildMember.Roles
                        .Select(roleId => roles.GetValueOrDefault(roleId.Value))
                        .Where(role => role is not null)
                        .Cast<Bot.Api.IRole>()
                        .ToArray();

                    members[memberId] = new RemoraMember(memberId, displayName, isBot, memberRoles, m_userApi, m_channelApi);
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

        private bool IsEveryoneRole(Bot.Api.IRole role)
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
