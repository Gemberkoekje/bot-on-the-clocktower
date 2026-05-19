using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Bot.Remora;
using Moq;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;
using Remora.Results;
using Xunit;
using DiscordChannel = Remora.Discord.API.Abstractions.Objects.IChannel;
using DiscordRole = Remora.Discord.API.Abstractions.Objects.IRole;

namespace Test.Bot.Remora
{
    public class TestRemoraGuildRuntime
    {
        [Fact]
        public static void Guild_ReadPaths_UseRestBackedData()
        {
            Mock<IDiscordRestGuildAPI> guildApi = new();
            ulong guildId = 42;
            SetupRoles(guildApi, guildId, BuildRole(guildId, "@everyone"), BuildRole(10, "storyteller", botRoleId: 777));
            SetupChannels(guildApi, guildId,
                BuildChannel(100, "day", ChannelType.GuildCategory),
                BuildChannel(101, "town-square", ChannelType.GuildVoice, parentId: 100, position: 5),
                BuildChannel(102, "control", ChannelType.GuildText, parentId: 100, position: 6));
            SetupMembersSinglePage(guildApi, guildId,
                BuildGuildMember(201, "alice", "Alice", new[] { 10UL }),
                BuildGuildMember(202, "bob", null, Array.Empty<ulong>()));

            RemoraGuild guild = new(guildId, "guild-42", guildApi.Object);

            Assert.Equal(2, guild.Roles.Count);
            Assert.NotNull(guild.GetRoleByName("storyteller"));
            Assert.NotNull(guild.BotRole);
            Assert.Equal(guildId, guild.EveryoneRole.Id);

            Assert.Equal(2, guild.Channels.Count);
            Assert.NotNull(guild.GetChannel(101));
            Assert.NotNull(guild.GetChannelByName("town-square"));

            var dayCategory = guild.GetCategoryByName("day");
            Assert.NotNull(dayCategory);
            Assert.Equal(2, dayCategory!.Channels.Count);
            Assert.NotNull(guild.GetChannelCategory(100));

            Assert.Equal(2, guild.Members.Count);
            Assert.True(guild.Members.ContainsKey(201));
            Assert.Equal("Alice", guild.Members[201].DisplayName);

            guildApi.Verify(api => api.GetGuildRolesAsync(new Snowflake(guildId), It.IsAny<CancellationToken>()), Times.Once);
            guildApi.Verify(api => api.GetGuildChannelsAsync(new Snowflake(guildId), It.IsAny<CancellationToken>()), Times.Once);
            guildApi.Verify(api => api.ListGuildMembersAsync(
                new Snowflake(guildId),
                It.IsAny<Optional<int>>(),
                It.IsAny<Optional<Snowflake>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public static void Guild_ReadLookups_NotFoundReturnsNull()
        {
            Mock<IDiscordRestGuildAPI> guildApi = new();
            ulong guildId = 99;
            SetupRoles(guildApi, guildId);
            SetupChannels(guildApi, guildId);
            SetupMembersSinglePage(guildApi, guildId);

            RemoraGuild guild = new(guildId, "guild-99", guildApi.Object);

            Assert.Null(guild.GetRoleByName("missing"));
            Assert.Null(guild.GetChannel(500));
            Assert.Null(guild.GetChannelByName("missing"));
            Assert.Null(guild.GetChannelCategory(600));
            Assert.Null(guild.GetCategoryByName("missing"));
            Assert.Empty(guild.Members);
        }

        [Fact]
        public static void Guild_MembersRead_HandlesPaging()
        {
            Mock<IDiscordRestGuildAPI> guildApi = new();
            ulong guildId = 111;
            SetupRoles(guildApi, guildId, BuildRole(guildId, "@everyone"));
            SetupChannels(guildApi, guildId);

            IReadOnlyList<IGuildMember> firstPage = Enumerable.Range(1, 1000)
                .Select(index => BuildGuildMember((ulong)index, $"user-{index}", null, Array.Empty<ulong>()))
                .ToArray();
            IReadOnlyList<IGuildMember> secondPage = new[] { BuildGuildMember(1001, "user-1001", null, Array.Empty<ulong>()) };

            guildApi
                .Setup(api => api.ListGuildMembersAsync(
                    new Snowflake(guildId),
                    It.Is<Optional<int>>(limit => limit.HasValue && limit.Value == 1000),
                    It.Is<Optional<Snowflake>>(after => !after.HasValue),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<IGuildMember>>.FromSuccess(firstPage));

            guildApi
                .Setup(api => api.ListGuildMembersAsync(
                    new Snowflake(guildId),
                    It.Is<Optional<int>>(limit => limit.HasValue && limit.Value == 1000),
                    It.Is<Optional<Snowflake>>(after => after.HasValue && after.Value.Value == 1000),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<IGuildMember>>.FromSuccess(secondPage));

            RemoraGuild guild = new(guildId, "guild-111", guildApi.Object);

            Assert.Equal(1001, guild.Members.Count);
            guildApi.Verify(api => api.ListGuildMembersAsync(
                new Snowflake(guildId),
                It.IsAny<Optional<int>>(),
                It.IsAny<Optional<Snowflake>>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        private static void SetupRoles(Mock<IDiscordRestGuildAPI> guildApi, ulong guildId, params DiscordRole[] roles)
        {
            guildApi
                .Setup(api => api.GetGuildRolesAsync(new Snowflake(guildId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<DiscordRole>>.FromSuccess(roles));
        }

        private static void SetupChannels(Mock<IDiscordRestGuildAPI> guildApi, ulong guildId, params DiscordChannel[] channels)
        {
            guildApi
                .Setup(api => api.GetGuildChannelsAsync(new Snowflake(guildId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<DiscordChannel>>.FromSuccess(channels));
        }

        private static void SetupMembersSinglePage(Mock<IDiscordRestGuildAPI> guildApi, ulong guildId, params IGuildMember[] members)
        {
            guildApi
                .Setup(api => api.ListGuildMembersAsync(
                    new Snowflake(guildId),
                    It.IsAny<Optional<int>>(),
                    It.IsAny<Optional<Snowflake>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<IReadOnlyList<IGuildMember>>.FromSuccess(members));
        }

        private static DiscordRole BuildRole(ulong id, string name, ulong botRoleId = 0)
        {
            Mock<global::Remora.Discord.API.Abstractions.Objects.IRole> role = new();
            role.SetupGet(r => r.ID).Returns(new Snowflake(id));
            role.SetupGet(r => r.Name).Returns(name);
            role.SetupGet(r => r.Tags).Returns(botRoleId == 0
                ? default
                : new Optional<IRoleTags>(BuildRoleTags(botRoleId)));
            return role.Object;
        }

        private static IRoleTags BuildRoleTags(ulong botRoleId)
        {
            Mock<IRoleTags> tags = new();
            tags.SetupGet(t => t.BotID).Returns(new Optional<Snowflake>(new Snowflake(botRoleId)));
            return tags.Object;
        }

        private static DiscordChannel BuildChannel(ulong id, string name, ChannelType type, ulong parentId = 0, int position = 0)
        {
            Mock<DiscordChannel> channel = new();
            channel.SetupGet(c => c.ID).Returns(new Snowflake(id));
            channel.SetupGet(c => c.Type).Returns(type);
            channel.SetupGet(c => c.Name).Returns(new Optional<string?>(name));
            channel.SetupGet(c => c.Position).Returns(new Optional<int>(position));
            channel.SetupGet(c => c.ParentID).Returns(parentId == 0
                ? default
                : new Optional<Snowflake?>(new Snowflake(parentId)));
            return channel.Object;
        }

        private static IGuildMember BuildGuildMember(ulong id, string username, string? nickname, IReadOnlyCollection<ulong> roleIds)
        {
            Mock<IGuildMember> member = new();
            member.SetupGet(m => m.User).Returns(new Optional<IUser>(BuildUser(id, username)));
            member.SetupGet(m => m.Nickname).Returns(nickname is null
                ? default
                : new Optional<string?>(nickname));
            member.SetupGet(m => m.Roles).Returns(roleIds.Select(roleId => new Snowflake(roleId)).ToArray());
            return member.Object;
        }

        private static IUser BuildUser(ulong id, string username)
        {
            Mock<IUser> user = new();
            user.SetupGet(u => u.ID).Returns(new Snowflake(id));
            user.SetupGet(u => u.Username).Returns(username);
            user.SetupGet(u => u.IsBot).Returns(new Optional<bool>(false));
            return user.Object;
        }
    }
}
