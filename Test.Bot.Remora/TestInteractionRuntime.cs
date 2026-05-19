using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bot.Api;
using Bot.Remora;
using Moq;
using OneOf;
using global::Remora.Discord.API.Abstractions.Objects;
using global::Remora.Discord.API.Abstractions.Rest;
using global::Remora.Discord.API.Objects;
using global::Remora.Rest.Core;
using global::Remora.Results;
using Xunit;

namespace Test.Bot.Remora
{
    public class TestInteractionRuntime
    {

        [Fact]
        public static async Task ResolvedAdapters_MutatingOperationsThrowNotSupported()
        {
            global::Bot.Api.IMember member = new ResolvedMemberAdapter(
                BuildUser(100, "user"),
                nickname: new Optional<string?>("display"),
                roleIds: default,
                resolvedRoles: new Dictionary<Snowflake, global::Remora.Discord.API.Abstractions.Objects.IRole>());
            global::Bot.Api.IRole role = new ResolvedRoleAdapter(BuildRole(200, "role"));
            global::Bot.Api.IChannel channel = new ResolvedChannelAdapter(BuildChannel(300, "channel", ChannelType.GuildText));
            global::Bot.Api.IChannelCategory category = new ResolvedChannelCategoryAdapter(BuildChannel(400, "category", ChannelType.GuildCategory));

            await Assert.ThrowsAsync<NotSupportedException>(() => member.MoveToChannelAsync(channel));
            await Assert.ThrowsAsync<NotSupportedException>(() => member.GrantRoleAsync(role));
            await Assert.ThrowsAsync<NotSupportedException>(() => member.RevokeRoleAsync(role));
            await Assert.ThrowsAsync<NotSupportedException>(() => member.SendMessageAsync("test"));
            await Assert.ThrowsAsync<NotSupportedException>(() => member.SetDisplayName("renamed"));
            await Assert.ThrowsAsync<NotSupportedException>(() => role.DeleteAsync());

            await Assert.ThrowsAsync<NotSupportedException>(() => channel.AddOverwriteAsync(member, global::Bot.Api.IBaseChannel.Permissions.AccessChannels));
            await Assert.ThrowsAsync<NotSupportedException>(() => channel.AddOverwriteAsync(role, global::Bot.Api.IBaseChannel.Permissions.AccessChannels));
            await Assert.ThrowsAsync<NotSupportedException>(() => channel.RemoveOverwriteAsync(role));
            await Assert.ThrowsAsync<NotSupportedException>(() => channel.SendMessageAsync("test"));
            await Assert.ThrowsAsync<NotSupportedException>(() => channel.SendMessageAsync(Mock.Of<global::Bot.Api.IEmbed>()));
            await Assert.ThrowsAsync<NotSupportedException>(() => channel.SendMessageAsync(Mock.Of<global::Bot.Api.IMessageBuilder>()));
            await Assert.ThrowsAsync<NotSupportedException>(() => channel.RestrictOverwriteToMembersAsync(
                Array.Empty<global::Bot.Api.IMember>(),
                global::Bot.Api.IBaseChannel.Permissions.AccessChannels,
                Array.Empty<global::Bot.Api.IMember>()));
            await Assert.ThrowsAsync<NotSupportedException>(() => channel.DeleteAsync());

            await Assert.ThrowsAsync<NotSupportedException>(() => category.AddOverwriteAsync(member, global::Bot.Api.IBaseChannel.Permissions.AccessChannels));
            await Assert.ThrowsAsync<NotSupportedException>(() => category.AddOverwriteAsync(role, global::Bot.Api.IBaseChannel.Permissions.AccessChannels));
            await Assert.ThrowsAsync<NotSupportedException>(() => category.RemoveOverwriteAsync(role));
            await Assert.ThrowsAsync<NotSupportedException>(() => category.DeleteAsync());
            Assert.Null(category.GetChannelByName("missing"));
        }

        [Fact]
        public static async Task Responder_RoutesSlashToDispatcher_AndIgnoresNonSlash()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            RecordingDispatcher dispatcher = new();
            RecordingComponentDispatcher componentDispatcher = new();
            Mock<IDiscordRestInteractionAPI> interactionApi = CreateInteractionApiMock();
            RemoraInteractionResponder responder = new(dispatcher, componentDispatcher, interactionApi.Object);

            Mock<IInteraction> slashInteraction = CreateSlashInteraction("test");
            await responder.RespondAsync(slashInteraction.Object, cancellationToken);
            Assert.Equal(1, dispatcher.CallCount);
            Assert.Same(slashInteraction.Object, dispatcher.LastInteraction);
            Assert.Equal(0, componentDispatcher.CallCount);

            Mock<IInteraction> componentInteraction = CreateMessageComponentInteraction("component", "value");
            await responder.RespondAsync(componentInteraction.Object, cancellationToken);
            Assert.Equal(1, componentDispatcher.CallCount);

            Mock<IInteraction> modalInteraction = CreateModalSubmitInteraction("modal-id", ("field-1", "abc"));
            await responder.RespondAsync(modalInteraction.Object, cancellationToken);
            Assert.Equal(2, componentDispatcher.CallCount);
        }

        [Fact]
        public static async Task Responder_ComponentUnknown_ShouldSendEphemeralFallback()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            RecordingDispatcher slashDispatcher = new();
            RecordingComponentDispatcher componentDispatcher = new() { ReturnValue = false };
            Mock<IDiscordRestInteractionAPI> interactionApi = CreateInteractionApiMock();
            RemoraInteractionResponder responder = new(slashDispatcher, componentDispatcher, interactionApi.Object);
            Mock<IInteraction> componentInteraction = CreateMessageComponentInteraction("unknown-component");

            await responder.RespondAsync(componentInteraction.Object, cancellationToken);

            interactionApi.Verify(
                api => api.CreateInteractionResponseAsync(
                    componentInteraction.Object.ID,
                    componentInteraction.Object.Token,
                    It.Is<IInteractionResponse>(response => IsEphemeralErrorResponse(response)),
                    default,
                    cancellationToken),
                Times.Once);
        }

        [Fact]
        public static async Task ComponentDispatcher_DispatchesKnownAndUnknownCustomIds()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            Mock<IComponentService> componentService = new();
            Mock<IDiscordRestInteractionAPI> interactionApi = CreateInteractionApiMock();
            RemoraComponentDispatcher dispatcher = new(componentService.Object, interactionApi.Object);
            IBotInteractionContext? seenContext = null;

            componentService
                .Setup(service => service.CallAsync(It.IsAny<IBotInteractionContext>()))
                .Returns<IBotInteractionContext>(context =>
                {
                    seenContext = context;
                    return Task.FromResult(context.ComponentCustomId == "known-component");
                });

            bool knownResult = await dispatcher.DispatchAsync(
                CreateMessageComponentInteraction("known-component", "value-1", "value-2").Object,
                cancellationToken);
            bool unknownResult = await dispatcher.DispatchAsync(
                CreateMessageComponentInteraction("unknown-component").Object,
                cancellationToken);

            Assert.True(knownResult);
            Assert.False(unknownResult);
            Assert.NotNull(seenContext);
            Assert.Equal("unknown-component", seenContext!.ComponentCustomId);
        }

        [Fact]
        public static async Task ComponentDispatcher_DispatchesModalSubmissionValues()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            Mock<IComponentService> componentService = new();
            Mock<IDiscordRestInteractionAPI> interactionApi = CreateInteractionApiMock();
            RemoraComponentDispatcher dispatcher = new(componentService.Object, interactionApi.Object);
            IBotInteractionContext? seenContext = null;

            componentService
                .Setup(service => service.CallAsync(It.IsAny<IBotInteractionContext>()))
                .Returns<IBotInteractionContext>(context =>
                {
                    seenContext = context;
                    return Task.FromResult(true);
                });

            bool handled = await dispatcher.DispatchAsync(
                CreateModalSubmitInteraction(
                    "modal-submit",
                    ("field-1", "value-a"),
                    ("field-2", "value-b")).Object,
                cancellationToken);

            Assert.True(handled);
            Assert.NotNull(seenContext);
            Assert.Equal("modal-submit", seenContext!.ComponentCustomId);
            Assert.Equal(new[] { "value-a", "value-b" }, seenContext.ComponentValues);
        }

        [Fact]
        public static async Task LiveContext_DeferThenEdit_CallsExpectedRestMethods()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            Mock<IDiscordRestInteractionAPI> interactionApi = CreateInteractionApiMock();
            LiveRemoraInteractionContext context = new(
                new RemoraGuild(1, "guild"),
                new RemoraChannel(2, "channel"),
                new RemoraMember(3, "member"),
                interactionApi.Object,
                new Snowflake(100),
                new Snowflake(200),
                "interaction-token",
                cancellationToken);

            RemoraWebhookBuilder webhook = new();
            webhook.WithContent("edited-content");

            await context.DeferInteractionResponse();
            await context.EditResponseAsync(webhook);

            interactionApi.Verify(
                api => api.CreateInteractionResponseAsync(
                    new Snowflake(200),
                    "interaction-token",
                    It.Is<IInteractionResponse>(r => r.Type == InteractionCallbackType.DeferredChannelMessageWithSource),
                    default,
                    cancellationToken),
                Times.Once);

            interactionApi.Verify(
                api => api.EditOriginalInteractionResponseAsync(
                    new Snowflake(100),
                    "interaction-token",
                    It.Is<Optional<string?>>(content => content.HasValue && content.Value == "edited-content"),
                    default,
                    default,
                    default,
                    default,
                    default,
                    cancellationToken),
                Times.Once);
        }

        [Fact]
        public static async Task LiveContext_DoubleDefer_IsIdempotent()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            Mock<IDiscordRestInteractionAPI> interactionApi = CreateInteractionApiMock();
            LiveRemoraInteractionContext context = new(
                new RemoraGuild(1, "guild"),
                new RemoraChannel(2, "channel"),
                new RemoraMember(3, "member"),
                interactionApi.Object,
                new Snowflake(100),
                new Snowflake(200),
                "interaction-token",
                cancellationToken);

            await context.DeferInteractionResponse();
            await context.DeferInteractionResponse();

            interactionApi.Verify(
                api => api.CreateInteractionResponseAsync(
                    new Snowflake(200),
                    "interaction-token",
                    It.Is<IInteractionResponse>(r => r.Type == InteractionCallbackType.DeferredChannelMessageWithSource),
                    default,
                    cancellationToken),
                Times.Once);
        }

        [Fact]
        public static async Task LiveContext_EditBeforeDefer_AutoDefers()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            Mock<IDiscordRestInteractionAPI> interactionApi = CreateInteractionApiMock();
            LiveRemoraInteractionContext context = new(
                new RemoraGuild(1, "guild"),
                new RemoraChannel(2, "channel"),
                new RemoraMember(3, "member"),
                interactionApi.Object,
                new Snowflake(100),
                new Snowflake(200),
                "interaction-token",
                cancellationToken);

            RemoraWebhookBuilder webhook = new();
            webhook.WithContent("edited-content");
            await context.EditResponseAsync(webhook);

            interactionApi.Verify(
                api => api.CreateInteractionResponseAsync(
                    new Snowflake(200),
                    "interaction-token",
                    It.Is<IInteractionResponse>(r => r.Type == InteractionCallbackType.DeferredChannelMessageWithSource),
                    default,
                    cancellationToken),
                Times.Once);

            interactionApi.Verify(
                api => api.EditOriginalInteractionResponseAsync(
                    new Snowflake(100),
                    "interaction-token",
                    It.Is<Optional<string?>>(content => content.HasValue && content.Value == "edited-content"),
                    default,
                    default,
                    default,
                    default,
                    default,
                    cancellationToken),
                Times.Once);
        }

        [Fact]
        public static async Task LiveContext_UpdateOriginalMessage_CallsUpdateInteractionResponse()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            Mock<IDiscordRestInteractionAPI> interactionApi = CreateInteractionApiMock();
            LiveRemoraInteractionContext context = new(
                new RemoraGuild(1, "guild"),
                new RemoraChannel(2, "channel"),
                new RemoraMember(3, "member"),
                interactionApi.Object,
                new Snowflake(100),
                new Snowflake(200),
                "interaction-token",
                cancellationToken);

            RemoraInteractionResponseBuilder builder = new();
            builder.WithContent("updated-content");
            builder.AddComponents(RemoraComponent.Button("button-id", "Press", IBotSystem.ButtonType.Primary, false, string.Empty));

            await context.UpdateOriginalMessageAsync(builder);

            interactionApi.Verify(
                api => api.CreateInteractionResponseAsync(
                    new Snowflake(200),
                    "interaction-token",
                    It.Is<IInteractionResponse>(response => response.Type == InteractionCallbackType.UpdateMessage),
                    default,
                    cancellationToken),
                Times.Once);
        }

        [Fact]
        public static async Task LiveContext_ShowModal_CallsModalInteractionResponse()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            Mock<IDiscordRestInteractionAPI> interactionApi = CreateInteractionApiMock();
            LiveRemoraInteractionContext context = new(
                new RemoraGuild(1, "guild"),
                new RemoraChannel(2, "channel"),
                new RemoraMember(3, "member"),
                interactionApi.Object,
                new Snowflake(100),
                new Snowflake(200),
                "interaction-token",
                cancellationToken);

            RemoraInteractionResponseBuilder builder = new();
            builder.WithCustomId("modal-id");
            builder.WithTitle("Modal Title");
            builder.AddComponents(RemoraComponent.TextInput("field-id", "Field Label", "Placeholder", "Initial value", true));

            await context.ShowModalAsync(builder);

            interactionApi.Verify(
                api => api.CreateInteractionResponseAsync(
                    new Snowflake(200),
                    "interaction-token",
                    It.Is<IInteractionResponse>(response => response.Type == InteractionCallbackType.Modal),
                    default,
                    cancellationToken),
                Times.Once);
        }

        [Fact]
        public static async Task Responder_DispatchFailure_SendsEphemeralErrorAndDoesNotThrow()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            ThrowingDispatcher dispatcher = new();
            RecordingComponentDispatcher componentDispatcher = new();
            Mock<IDiscordRestInteractionAPI> interactionApi = CreateInteractionApiMock();
            RemoraInteractionResponder responder = new(dispatcher, componentDispatcher, interactionApi.Object);
            Mock<IInteraction> slashInteraction = CreateSlashInteraction("test");

            await responder.RespondAsync(slashInteraction.Object, cancellationToken);

            interactionApi.Verify(
                api => api.CreateInteractionResponseAsync(
                    slashInteraction.Object.ID,
                    slashInteraction.Object.Token,
                    It.Is<IInteractionResponse>(response => IsEphemeralErrorResponse(response)),
                    default,
                    cancellationToken),
                Times.Once);
        }

        private static bool IsEphemeralErrorResponse(IInteractionResponse response)
        {
            if (response.Type != InteractionCallbackType.ChannelMessageWithSource || !response.Data.HasValue)
            {
                return false;
            }

            if (!response.Data.Value.TryPickT0(out IInteractionMessageCallbackData callbackData, out _))
            {
                return false;
            }

            return callbackData.Flags.HasValue
                && callbackData.Flags.Value.HasFlag(MessageFlags.Ephemeral)
                && callbackData.Content.HasValue
                && !string.IsNullOrWhiteSpace(callbackData.Content.Value);
        }

        private static Mock<IDiscordRestInteractionAPI> CreateInteractionApiMock()
        {
            Mock<IDiscordRestInteractionAPI> interactionApi = new();

            interactionApi
                .Setup(api => api.CreateInteractionResponseAsync(
                    It.IsAny<Snowflake>(),
                    It.IsAny<string>(),
                    It.IsAny<IInteractionResponse>(),
                    It.IsAny<Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.FromSuccess());

            interactionApi
                .Setup(api => api.EditOriginalInteractionResponseAsync(
                    It.IsAny<Snowflake>(),
                    It.IsAny<string>(),
                    It.IsAny<Optional<string?>>(),
                    It.IsAny<Optional<IReadOnlyList<global::Remora.Discord.API.Abstractions.Objects.IEmbed>?>>(),
                    It.IsAny<Optional<IAllowedMentions?>>(),
                    It.IsAny<Optional<IReadOnlyList<IMessageComponent>?>>(),
                    It.IsAny<Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>?>>(),
                    It.IsAny<Optional<MessageFlags>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<global::Remora.Discord.API.Abstractions.Objects.IMessage>.FromSuccess(Mock.Of<global::Remora.Discord.API.Abstractions.Objects.IMessage>()));

            interactionApi
                .Setup(api => api.CreateFollowupMessageAsync(
                    It.IsAny<Snowflake>(),
                    It.IsAny<string>(),
                    It.IsAny<Optional<string>>(),
                    It.IsAny<Optional<bool>>(),
                    It.IsAny<Optional<IReadOnlyList<global::Remora.Discord.API.Abstractions.Objects.IEmbed>>>(),
                    It.IsAny<Optional<IAllowedMentions>>(),
                    It.IsAny<Optional<IReadOnlyList<IMessageComponent>>>(),
                    It.IsAny<Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>>>(),
                    It.IsAny<Optional<MessageFlags>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<global::Remora.Discord.API.Abstractions.Objects.IMessage>.FromSuccess(Mock.Of<global::Remora.Discord.API.Abstractions.Objects.IMessage>()));

            return interactionApi;
        }

        private static Mock<IInteraction> CreateSlashInteraction(string name, params IApplicationCommandInteractionDataOption[] options)
        {
            return CreateSlashInteraction(name, default, options);
        }

        private static Mock<IInteraction> CreateSlashInteraction(
            string name,
            Optional<IApplicationCommandInteractionDataResolved> resolved,
            params IApplicationCommandInteractionDataOption[] options)
        {
            Mock<IApplicationCommandData> data = new();
            data.SetupGet(d => d.Name).Returns(name);
            data.SetupGet(d => d.Options).Returns(new Optional<IReadOnlyList<IApplicationCommandInteractionDataOption>>(options));
            data.SetupGet(d => d.Resolved).Returns(resolved);

            Mock<IInteraction> interaction = CreateInteraction(InteractionType.ApplicationCommand);
            interaction.SetupGet(i => i.Data).Returns(new Optional<OneOf<IApplicationCommandData, IMessageComponentData, IModalSubmitData>>(
                OneOf<IApplicationCommandData, IMessageComponentData, IModalSubmitData>.FromT0(data.Object)));
            return interaction;
        }

        private static Mock<IInteraction> CreateMessageComponentInteraction(string customId, params string[] values)
        {
            MessageComponentData data = new(
                customId,
                ComponentType.StringSelect,
                default,
                values.Length == 0
                    ? default
                    : new Optional<OneOf<IReadOnlyList<Snowflake>, IReadOnlyList<string>>>(values));

            Mock<IInteraction> interaction = CreateInteraction(InteractionType.MessageComponent);
            interaction.SetupGet(i => i.Data).Returns(new Optional<OneOf<IApplicationCommandData, IMessageComponentData, IModalSubmitData>>(
                OneOf<IApplicationCommandData, IMessageComponentData, IModalSubmitData>.FromT1(data)));
            return interaction;
        }

        private static Mock<IInteraction> CreateModalSubmitInteraction(string customId, params (string fieldId, string value)[] fields)
        {
            IReadOnlyList<IPartialMessageComponent> fieldComponents = fields
                .Select(field => (IPartialMessageComponent)new PartialTextInputComponent(
                    new Optional<string>(field.fieldId),
                    new Optional<string>(field.value),
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default))
                .ToArray();

            PartialActionRowComponent row = new(new Optional<IReadOnlyList<IPartialMessageComponent>>(fieldComponents), default);
            ModalSubmitData data = new(customId, new IPartialMessageComponent[] { row });

            Mock<IInteraction> interaction = CreateInteraction(InteractionType.ModalSubmit);
            interaction.SetupGet(i => i.Data).Returns(new Optional<OneOf<IApplicationCommandData, IMessageComponentData, IModalSubmitData>>(
                OneOf<IApplicationCommandData, IMessageComponentData, IModalSubmitData>.FromT2(data)));
            return interaction;
        }

        private static Mock<IInteraction> CreateInteraction(InteractionType type)
        {
            Mock<IInteraction> interaction = new();
            interaction.SetupGet(i => i.ID).Returns(new Snowflake(111));
            interaction.SetupGet(i => i.ApplicationID).Returns(new Snowflake(222));
            interaction.SetupGet(i => i.Token).Returns("interaction-token");
            interaction.SetupGet(i => i.Type).Returns(type);
            interaction.SetupGet(i => i.GuildID).Returns(new Optional<Snowflake>(new Snowflake(1)));
            interaction.SetupGet(i => i.Channel).Returns(default(Optional<IPartialChannel>));
            interaction.SetupGet(i => i.Member).Returns(default(Optional<IGuildMember>));
            interaction.SetupGet(i => i.User).Returns(default(Optional<IUser>));
            interaction.SetupGet(i => i.Data).Returns(default(Optional<OneOf<IApplicationCommandData, IMessageComponentData, IModalSubmitData>>));
            return interaction;
        }

        private static IApplicationCommandInteractionDataOption BuildOption(string name, string value)
        {
            return new ApplicationCommandInteractionDataOption(
                name,
                ApplicationCommandOptionType.String,
                new Optional<OneOf<string, long, bool, Snowflake, double>>(value),
                default,
                default);
        }

        private static IApplicationCommandInteractionDataOption BuildOption(string name, bool value)
        {
            return new ApplicationCommandInteractionDataOption(
                name,
                ApplicationCommandOptionType.Boolean,
                new Optional<OneOf<string, long, bool, Snowflake, double>>(value),
                default,
                default);
        }

        private static IApplicationCommandInteractionDataOption BuildOption(string name, long value)
        {
            return new ApplicationCommandInteractionDataOption(
                name,
                ApplicationCommandOptionType.Integer,
                new Optional<OneOf<string, long, bool, Snowflake, double>>(value),
                default,
                default);
        }

        private static IApplicationCommandInteractionDataOption BuildSnowflakeOption(string name, ApplicationCommandOptionType type, ulong value)
        {
            return new ApplicationCommandInteractionDataOption(
                name,
                type,
                new Optional<OneOf<string, long, bool, Snowflake, double>>(new Snowflake(value)),
                default,
                default);
        }

        private static IUser BuildUser(ulong id, string username)
        {
            Mock<IUser> user = new();
            user.SetupGet(u => u.ID).Returns(new Snowflake(id));
            user.SetupGet(u => u.Username).Returns(username);
            user.SetupGet(u => u.IsBot).Returns(new Optional<bool>(false));
            return user.Object;
        }

        private static IPartialGuildMember BuildPartialGuildMember(Optional<string?> nickname, Optional<IReadOnlyList<Snowflake>> roleIds)
        {
            Mock<IPartialGuildMember> member = new();
            member.SetupGet(m => m.Nickname).Returns(nickname);
            member.SetupGet(m => m.Roles).Returns(roleIds);
            return member.Object;
        }

        private static global::Remora.Discord.API.Abstractions.Objects.IRole BuildRole(ulong id, string name)
        {
            Mock<global::Remora.Discord.API.Abstractions.Objects.IRole> role = new();
            role.SetupGet(r => r.ID).Returns(new Snowflake(id));
            role.SetupGet(r => r.Name).Returns(name);
            return role.Object;
        }

        private static IPartialChannel BuildChannel(ulong id, string name, ChannelType type)
        {
            Mock<IPartialChannel> channel = new();
            channel.SetupGet(c => c.ID).Returns(new Optional<Snowflake>(new Snowflake(id)));
            channel.SetupGet(c => c.Name).Returns(new Optional<string?>(name));
            channel.SetupGet(c => c.Type).Returns(new Optional<ChannelType>(type));
            channel.SetupGet(c => c.Position).Returns(new Optional<int>(1));
            return channel.Object;
        }

        private static IApplicationCommandInteractionDataResolved CreateResolvedData(
            IReadOnlyDictionary<Snowflake, IUser> users,
            IReadOnlyDictionary<Snowflake, IPartialGuildMember> members,
            IReadOnlyDictionary<Snowflake, global::Remora.Discord.API.Abstractions.Objects.IRole> roles,
            IReadOnlyDictionary<Snowflake, IPartialChannel> channels)
        {
            Mock<IApplicationCommandInteractionDataResolved> resolved = new();
            resolved.SetupGet(r => r.Users).Returns(new Optional<IReadOnlyDictionary<Snowflake, IUser>>(users));
            resolved.SetupGet(r => r.Members).Returns(new Optional<IReadOnlyDictionary<Snowflake, IPartialGuildMember>>(members));
            resolved.SetupGet(r => r.Roles).Returns(new Optional<IReadOnlyDictionary<Snowflake, global::Remora.Discord.API.Abstractions.Objects.IRole>>(roles));
            resolved.SetupGet(r => r.Channels).Returns(new Optional<IReadOnlyDictionary<Snowflake, IPartialChannel>>(channels));
            return resolved.Object;
        }

        private sealed class RecordingDispatcher : IRemoraSlashCommandDispatcher
        {
            public int CallCount { get; private set; }

            public IInteraction? LastInteraction { get; private set; }

            public Task DispatchAsync(IInteraction interaction, CancellationToken cancellationToken = default)
            {
                CallCount++;
                LastInteraction = interaction;
                return Task.CompletedTask;
            }
        }

        private sealed class ThrowingDispatcher : IRemoraSlashCommandDispatcher
        {
            public Task DispatchAsync(IInteraction interaction, CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("boom");
            }
        }

        private sealed class RecordingComponentDispatcher : IRemoraComponentDispatcher
        {
            public int CallCount { get; private set; }

            public bool ReturnValue { get; set; } = true;

            public Task<bool> DispatchAsync(IInteraction interaction, CancellationToken cancellationToken = default)
            {
                CallCount++;
                return Task.FromResult(ReturnValue);
            }
        }
    }
}
