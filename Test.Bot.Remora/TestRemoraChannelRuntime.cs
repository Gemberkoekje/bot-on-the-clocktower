using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bot.Api;
using Bot.Remora;
using Moq;
using OneOf;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;
using Xunit;
using DiscordEmbed = Remora.Discord.API.Abstractions.Objects.IEmbed;
using DiscordMessage = Remora.Discord.API.Abstractions.Objects.IMessage;

namespace Test.Bot.Remora
{
    public class TestRemoraChannelRuntime
    {
        [Fact]
        public static async Task Channel_WriteOperations_UseRestApis()
        {
            Mock<IDiscordRestChannelAPI> channelApi = CreateChannelApiMockWithSuccessfulDefaults();
            RemoraChannel channel = new(33, "control", channelApi: channelApi.Object);
            RemoraMember member = new(100, "member");
            RemoraRole role = new(200, "role");
            RemoraMessageBuilder builder = new();
            builder.WithContent("builder-content");

            await channel.SendMessageAsync("plain-content");
            await channel.SendMessageAsync(builder);
            await channel.SendMessageAsync(new RemoraEmbed());
            await channel.AddOverwriteAsync(member, IBaseChannel.Permissions.AccessChannels | IBaseChannel.Permissions.UseVoice);
            await channel.AddOverwriteAsync(role, IBaseChannel.Permissions.MoveMembers, IBaseChannel.Permissions.Stream);
            await channel.RemoveOverwriteAsync(role);
            await channel.DeleteAsync("cleanup");

            channelApi.Verify(
                api => api.CreateMessageAsync(
                    new Snowflake(33),
                    It.Is<Optional<string>>(value => value.HasValue && value.Value == "plain-content"),
                    It.IsAny<Optional<string>>(),
                    It.IsAny<Optional<bool>>(),
                    It.IsAny<Optional<IReadOnlyList<DiscordEmbed>>>(),
                    It.IsAny<Optional<IAllowedMentions>>(),
                    It.IsAny<Optional<IMessageReference>>(),
                    It.IsAny<Optional<IReadOnlyList<IMessageComponent>>>(),
                    It.IsAny<Optional<IReadOnlyList<Snowflake>>>(),
                    It.IsAny<Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>>>(),
                    It.IsAny<Optional<MessageFlags>>(),
                    It.IsAny<Optional<bool>>(),
                    It.IsAny<Optional<IPollCreateRequest>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            channelApi.Verify(
                api => api.CreateMessageAsync(
                    new Snowflake(33),
                    It.Is<Optional<string>>(value => value.HasValue && value.Value == "builder-content"),
                    It.IsAny<Optional<string>>(),
                    It.IsAny<Optional<bool>>(),
                    It.IsAny<Optional<IReadOnlyList<DiscordEmbed>>>(),
                    It.IsAny<Optional<IAllowedMentions>>(),
                    It.IsAny<Optional<IMessageReference>>(),
                    It.IsAny<Optional<IReadOnlyList<IMessageComponent>>>(),
                    It.IsAny<Optional<IReadOnlyList<Snowflake>>>(),
                    It.IsAny<Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>>>(),
                    It.IsAny<Optional<MessageFlags>>(),
                    It.IsAny<Optional<bool>>(),
                    It.IsAny<Optional<IPollCreateRequest>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            channelApi.Verify(
                api => api.CreateMessageAsync(
                    new Snowflake(33),
                    It.IsAny<Optional<string>>(),
                    It.IsAny<Optional<string>>(),
                    It.IsAny<Optional<bool>>(),
                    It.Is<Optional<IReadOnlyList<DiscordEmbed>>>(value => value.HasValue && value.Value.Count == 1),
                    It.IsAny<Optional<IAllowedMentions>>(),
                    It.IsAny<Optional<IMessageReference>>(),
                    It.IsAny<Optional<IReadOnlyList<IMessageComponent>>>(),
                    It.IsAny<Optional<IReadOnlyList<Snowflake>>>(),
                    It.IsAny<Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>>>(),
                    It.IsAny<Optional<MessageFlags>>(),
                    It.IsAny<Optional<bool>>(),
                    It.IsAny<Optional<IPollCreateRequest>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            channelApi.Verify(
                api => api.EditChannelPermissionsAsync(
                    new Snowflake(33),
                    new Snowflake(100),
                    It.Is<Optional<IDiscordPermissionSet?>>(allow => HasPermission(allow, DiscordPermission.ViewChannel)),
                    It.IsAny<Optional<IDiscordPermissionSet?>>(),
                    It.Is<Optional<PermissionOverwriteType>>(type => type.HasValue && type.Value == PermissionOverwriteType.Member),
                    It.IsAny<Optional<string>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            channelApi.Verify(
                api => api.EditChannelPermissionsAsync(
                    new Snowflake(33),
                    new Snowflake(200),
                    It.Is<Optional<IDiscordPermissionSet?>>(allow => HasPermission(allow, DiscordPermission.MoveMembers)),
                    It.Is<Optional<IDiscordPermissionSet?>>(deny => HasPermission(deny, DiscordPermission.Stream)),
                    It.Is<Optional<PermissionOverwriteType>>(type => type.HasValue && type.Value == PermissionOverwriteType.Role),
                    It.IsAny<Optional<string>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            channelApi.Verify(api => api.DeleteChannelPermissionAsync(new Snowflake(33), new Snowflake(200), It.IsAny<Optional<string>>(), It.IsAny<CancellationToken>()), Times.Once);
            channelApi.Verify(api => api.DeleteChannelAsync(new Snowflake(33), It.Is<Optional<string>>(reason => reason.HasValue && reason.Value == "cleanup"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public static async Task Channel_RestrictOverwriteToMembers_UpdatesAllowedAndOthers()
        {
            Mock<IDiscordRestChannelAPI> channelApi = CreateChannelApiMockWithSuccessfulDefaults();
            RemoraChannel channel = new(77, "night", channelApi: channelApi.Object);
            RemoraMember allowed = new(1, "allowed");
            RemoraMember removed = new(2, "removed");

            await channel.RestrictOverwriteToMembersAsync(
                new IMember[] { allowed, removed },
                IBaseChannel.Permissions.AccessChannels,
                new IMember[] { allowed });

            channelApi.Verify(
                api => api.EditChannelPermissionsAsync(
                    new Snowflake(77),
                    new Snowflake(1),
                    It.Is<Optional<IDiscordPermissionSet?>>(allow => HasPermission(allow, DiscordPermission.ViewChannel)),
                    It.IsAny<Optional<IDiscordPermissionSet?>>(),
                    It.Is<Optional<PermissionOverwriteType>>(type => type.HasValue && type.Value == PermissionOverwriteType.Member),
                    It.IsAny<Optional<string>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            channelApi.Verify(
                api => api.DeleteChannelPermissionAsync(
                    new Snowflake(77),
                    new Snowflake(2),
                    It.IsAny<Optional<string>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public static async Task Channel_WriteOperations_FailedRestCallsThrowInvalidOperation()
        {
            Mock<IDiscordRestChannelAPI> channelApi = CreateChannelApiMockWithSuccessfulDefaults();
            channelApi
                .Setup(api => api.CreateMessageAsync(
                    It.IsAny<Snowflake>(),
                    It.IsAny<Optional<string>>(),
                    It.IsAny<Optional<string>>(),
                    It.IsAny<Optional<bool>>(),
                    It.IsAny<Optional<IReadOnlyList<DiscordEmbed>>>(),
                    It.IsAny<Optional<IAllowedMentions>>(),
                    It.IsAny<Optional<IMessageReference>>(),
                    It.IsAny<Optional<IReadOnlyList<IMessageComponent>>>(),
                    It.IsAny<Optional<IReadOnlyList<Snowflake>>>(),
                    It.IsAny<Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>>>(),
                    It.IsAny<Optional<MessageFlags>>(),
                    It.IsAny<Optional<bool>>(),
                    It.IsAny<Optional<IPollCreateRequest>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<DiscordMessage>.FromError(new NotFoundError("message failed")));

            RemoraChannel channel = new(9, "error", channelApi: channelApi.Object);

            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => channel.SendMessageAsync("hello"));
            Assert.Contains("Failed to send channel message.", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public static async Task Channel_DeleteAsync_LogsTargetTypeAndName()
        {
            Mock<IDiscordRestChannelAPI> channelApi = CreateChannelApiMockWithSuccessfulDefaults();
            RemoraChannel channel = new(33, "control", channelApi: channelApi.Object);
            StringWriter writer = new();
            TextWriter? originalOut = Console.Out;

            try
            {
                Console.SetOut(writer);

                await channel.DeleteAsync("cleanup");
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            string output = writer.ToString();
            Assert.Contains("RemoraChannel: delete requested.", output, StringComparison.Ordinal);
            Assert.Contains("TargetType=Bot.Remora.RemoraChannel", output, StringComparison.Ordinal);
            Assert.Contains("Id=33", output, StringComparison.Ordinal);
            Assert.Contains("Name='control'", output, StringComparison.Ordinal);
            Assert.Contains("Reason='cleanup'", output, StringComparison.Ordinal);
            Assert.Contains("delete succeeded.", output, StringComparison.Ordinal);
        }

        [Fact]
        public static async Task ChannelCategory_DeleteAsync_LogsTargetTypeAndName()
        {
            Mock<IDiscordRestChannelAPI> channelApi = CreateChannelApiMockWithSuccessfulDefaults();
            RemoraChannelCategory category = new(44, "rooms", channelApi: channelApi.Object);
            StringWriter writer = new();
            TextWriter? originalOut = Console.Out;

            try
            {
                Console.SetOut(writer);

                await category.DeleteAsync("cleanup");
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            string output = writer.ToString();
            Assert.Contains("RemoraChannelCategory: delete requested.", output, StringComparison.Ordinal);
            Assert.Contains("TargetType=Bot.Remora.RemoraChannelCategory", output, StringComparison.Ordinal);
            Assert.Contains("Id=44", output, StringComparison.Ordinal);
            Assert.Contains("Name='rooms'", output, StringComparison.Ordinal);
            Assert.Contains("Reason='cleanup'", output, StringComparison.Ordinal);
            Assert.Contains("delete succeeded.", output, StringComparison.Ordinal);
            channelApi.Verify(api => api.DeleteChannelAsync(new Snowflake(44), It.Is<Optional<string>>(reason => reason.HasValue && reason.Value == "cleanup"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public static async Task ChannelCategory_WriteOperations_UseRestApis()
        {
            Mock<IDiscordRestChannelAPI> channelApi = CreateChannelApiMockWithSuccessfulDefaults();
            RemoraChannelCategory category = new(44, "rooms", channelApi: channelApi.Object);
            RemoraMember member = new(100, "member");
            RemoraRole role = new(200, "role");

            await category.AddOverwriteAsync(member, IBaseChannel.Permissions.AccessChannels | IBaseChannel.Permissions.UseVoice);
            await category.AddOverwriteAsync(role, IBaseChannel.Permissions.ManageChannels, IBaseChannel.Permissions.Stream);
            await category.RemoveOverwriteAsync(role);

            channelApi.Verify(
                api => api.EditChannelPermissionsAsync(
                    new Snowflake(44),
                    new Snowflake(100),
                    It.Is<Optional<IDiscordPermissionSet?>>(allow => HasPermission(allow, DiscordPermission.ViewChannel)),
                    It.IsAny<Optional<IDiscordPermissionSet?>>(),
                    It.Is<Optional<PermissionOverwriteType>>(type => type.HasValue && type.Value == PermissionOverwriteType.Member),
                    It.IsAny<Optional<string>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            channelApi.Verify(
                api => api.EditChannelPermissionsAsync(
                    new Snowflake(44),
                    new Snowflake(200),
                    It.Is<Optional<IDiscordPermissionSet?>>(allow => HasPermission(allow, DiscordPermission.ManageChannels)),
                    It.Is<Optional<IDiscordPermissionSet?>>(deny => HasPermission(deny, DiscordPermission.Stream)),
                    It.Is<Optional<PermissionOverwriteType>>(type => type.HasValue && type.Value == PermissionOverwriteType.Role),
                    It.IsAny<Optional<string>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            channelApi.Verify(api => api.DeleteChannelPermissionAsync(new Snowflake(44), new Snowflake(200), It.IsAny<Optional<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public static async Task Channel_DeleteAsync_RemovesChannelFromParentCategory()
        {
            RemoraChannelCategory category = new(44, "rooms");
            RemoraChannel channel = new(45, "room-1", isVoice: true, parentCategory: category);
            category.AddChannel(channel);

            await channel.DeleteAsync("cleanup");

            Assert.Empty(category.Channels);
        }

        private static Mock<IDiscordRestChannelAPI> CreateChannelApiMockWithSuccessfulDefaults()
        {
            Mock<IDiscordRestChannelAPI> channelApi = new();
            channelApi
                .Setup(api => api.CreateMessageAsync(
                    It.IsAny<Snowflake>(),
                    It.IsAny<Optional<string>>(),
                    It.IsAny<Optional<string>>(),
                    It.IsAny<Optional<bool>>(),
                    It.IsAny<Optional<IReadOnlyList<DiscordEmbed>>>(),
                    It.IsAny<Optional<IAllowedMentions>>(),
                    It.IsAny<Optional<IMessageReference>>(),
                    It.IsAny<Optional<IReadOnlyList<IMessageComponent>>>(),
                    It.IsAny<Optional<IReadOnlyList<Snowflake>>>(),
                    It.IsAny<Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>>>(),
                    It.IsAny<Optional<MessageFlags>>(),
                    It.IsAny<Optional<bool>>(),
                    It.IsAny<Optional<IPollCreateRequest>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<DiscordMessage>.FromSuccess(Mock.Of<DiscordMessage>()));
            channelApi
                .Setup(api => api.EditChannelPermissionsAsync(
                    It.IsAny<Snowflake>(),
                    It.IsAny<Snowflake>(),
                    It.IsAny<Optional<IDiscordPermissionSet?>>(),
                    It.IsAny<Optional<IDiscordPermissionSet?>>(),
                    It.IsAny<Optional<PermissionOverwriteType>>(),
                    It.IsAny<Optional<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.FromSuccess());
            channelApi
                .Setup(api => api.DeleteChannelPermissionAsync(
                    It.IsAny<Snowflake>(),
                    It.IsAny<Snowflake>(),
                    It.IsAny<Optional<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.FromSuccess());
            channelApi
                .Setup(api => api.DeleteChannelAsync(
                    It.IsAny<Snowflake>(),
                    It.IsAny<Optional<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.FromSuccess());

            return channelApi;
        }

        private static bool HasPermission(Optional<IDiscordPermissionSet?> permissions, DiscordPermission permission)
        {
            return permissions.HasValue && permissions.Value is not null && permissions.Value.HasPermission(permission);
        }
    }
}
