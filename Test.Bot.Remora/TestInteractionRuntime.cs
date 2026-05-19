using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        public static async Task Dispatcher_CommandNameResolution_IsCaseSensitiveAndUnknownThrows()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            RemoraSlashCommandRegistry registry = new();
            registry.AddSource(new StubSource(new RecordingCommand(
                "announce",
                new[] { new RemoraSlashCommandParameter("hearAnnouncements", "desc", RemoraSlashCommandParameterType.Boolean, true) })));

            Mock<IDiscordRestInteractionAPI> interactionApi = CreateInteractionApiMock();
            RemoraSlashCommandDispatcher dispatcher = new(registry, interactionApi.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync(
                CreateSlashInteraction("Announce", BuildOption("hearAnnouncements", true)).Object,
                cancellationToken));
            await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync(
                CreateSlashInteraction("missing-command", BuildOption("hearAnnouncements", true)).Object,
                cancellationToken));
        }

        [Fact]
        public static async Task Dispatcher_PrimitiveOptionBinding_PresentMissingWrongType()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            RecordingCommand command = new(
                "test",
                new[]
                {
                    new RemoraSlashCommandParameter("name", "desc", RemoraSlashCommandParameterType.String, true),
                    new RemoraSlashCommandParameter("enabled", "desc", RemoraSlashCommandParameterType.Boolean, false),
                    new RemoraSlashCommandParameter("count", "desc", RemoraSlashCommandParameterType.Integer, false),
                });

            RemoraSlashCommandRegistry registry = new();
            registry.AddSource(new StubSource(command));
            Mock<IDiscordRestInteractionAPI> interactionApi = CreateInteractionApiMock();
            RemoraSlashCommandDispatcher dispatcher = new(registry, interactionApi.Object);

            await dispatcher.DispatchAsync(CreateSlashInteraction(
                "test",
                BuildOption("name", "value"),
                BuildOption("enabled", true),
                BuildOption("count", 7L)).Object, cancellationToken);

            Assert.Equal("value", command.LastArguments["name"]);
            Assert.Equal(true, command.LastArguments["enabled"]);
            Assert.Equal(7L, command.LastArguments["count"]);

            await Assert.ThrowsAsync<ArgumentException>(() => dispatcher.DispatchAsync(
                CreateSlashInteraction("test", BuildOption("enabled", true)).Object,
                cancellationToken));
            await Assert.ThrowsAsync<ArgumentException>(() => dispatcher.DispatchAsync(
                CreateSlashInteraction("test", BuildOption("name", "value"), BuildOption("enabled", "wrong")).Object,
                cancellationToken));
        }

        [Fact]
        public static async Task Dispatcher_EntityOptionBinding_PresentAndOptional()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            RecordingCommand command = new(
                "entity",
                new[]
                {
                    new RemoraSlashCommandParameter("member", "desc", RemoraSlashCommandParameterType.User, true),
                    new RemoraSlashCommandParameter("role", "desc", RemoraSlashCommandParameterType.Role, true),
                    new RemoraSlashCommandParameter("chatChannel", "desc", RemoraSlashCommandParameterType.Channel, true),
                    new RemoraSlashCommandParameter("nightCategory", "desc", RemoraSlashCommandParameterType.Channel, false),
                    new RemoraSlashCommandParameter("optionalUser", "desc", RemoraSlashCommandParameterType.User, false),
                });

            RemoraSlashCommandRegistry registry = new();
            registry.AddSource(new StubSource(command));
            Mock<IDiscordRestInteractionAPI> interactionApi = CreateInteractionApiMock();
            RemoraSlashCommandDispatcher dispatcher = new(registry, interactionApi.Object);

            IApplicationCommandInteractionDataResolved resolved = CreateResolvedData(
                users: new Dictionary<Snowflake, IUser>
                {
                    [new Snowflake(11)] = BuildUser(11, "member-user"),
                },
                members: new Dictionary<Snowflake, IPartialGuildMember>
                {
                    [new Snowflake(11)] = BuildPartialGuildMember(
                        nickname: new Optional<string?>("member-nick"),
                        roleIds: new Optional<IReadOnlyList<Snowflake>>(new[] { new Snowflake(31) })),
                },
                roles: new Dictionary<Snowflake, global::Remora.Discord.API.Abstractions.Objects.IRole>
                {
                    [new Snowflake(21)] = BuildRole(21, "storyteller"),
                    [new Snowflake(31)] = BuildRole(31, "registered-role"),
                },
                channels: new Dictionary<Snowflake, IPartialChannel>
                {
                    [new Snowflake(41)] = BuildChannel(41, "chat", ChannelType.GuildText),
                    [new Snowflake(42)] = BuildChannel(42, "night", ChannelType.GuildCategory),
                });

            await dispatcher.DispatchAsync(
                CreateSlashInteraction(
                    "entity",
                    new Optional<IApplicationCommandInteractionDataResolved>(resolved),
                    BuildSnowflakeOption("member", ApplicationCommandOptionType.User, 11),
                    BuildSnowflakeOption("role", ApplicationCommandOptionType.Role, 21),
                    BuildSnowflakeOption("chatChannel", ApplicationCommandOptionType.Channel, 41),
                    BuildSnowflakeOption("nightCategory", ApplicationCommandOptionType.Channel, 42)).Object,
                cancellationToken);

            Assert.True(command.WasInvoked);
            Assert.IsType<ResolvedMemberAdapter>(command.LastArguments["member"]);
            Assert.IsType<ResolvedRoleAdapter>(command.LastArguments["role"]);
            Assert.IsType<ResolvedChannelAdapter>(command.LastArguments["chatChannel"]);
            Assert.IsType<ResolvedChannelCategoryAdapter>(command.LastArguments["nightCategory"]);
            Assert.False(command.LastArguments.ContainsKey("optionalUser"));

            global::Bot.Api.IMember member = Assert.IsAssignableFrom<global::Bot.Api.IMember>(command.LastArguments["member"]);
            Assert.Equal((ulong)11, member.Id);
            Assert.Equal("member-nick", member.DisplayName);
            Assert.Single(member.Roles);
            Assert.Equal((ulong)31, member.Roles.First().Id);

            global::Bot.Api.IRole role = Assert.IsAssignableFrom<global::Bot.Api.IRole>(command.LastArguments["role"]);
            Assert.Equal((ulong)21, role.Id);
            Assert.Equal("storyteller", role.Name);
        }

        [Fact]
        public static async Task Dispatcher_EntityOptionBinding_MissingResolvedAndWrongTypeThrows()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            RemoraSlashCommandRegistry registry = new();
            registry.AddSource(new StubSource(new RecordingCommand(
                "entity",
                new[] { new RemoraSlashCommandParameter("member", "desc", RemoraSlashCommandParameterType.User, true) })));

            Mock<IDiscordRestInteractionAPI> interactionApi = CreateInteractionApiMock();
            RemoraSlashCommandDispatcher dispatcher = new(registry, interactionApi.Object);

            IApplicationCommandInteractionDataResolved missingUserResolved = CreateResolvedData(
                users: new Dictionary<Snowflake, IUser>(),
                members: new Dictionary<Snowflake, IPartialGuildMember>(),
                roles: new Dictionary<Snowflake, global::Remora.Discord.API.Abstractions.Objects.IRole>(),
                channels: new Dictionary<Snowflake, IPartialChannel>());

            await Assert.ThrowsAsync<ArgumentException>(() => dispatcher.DispatchAsync(
                CreateSlashInteraction(
                    "entity",
                    new Optional<IApplicationCommandInteractionDataResolved>(missingUserResolved),
                    BuildSnowflakeOption("member", ApplicationCommandOptionType.User, 11)).Object,
                cancellationToken));

            await Assert.ThrowsAsync<ArgumentException>(() => dispatcher.DispatchAsync(
                CreateSlashInteraction(
                    "entity",
                    BuildOption("member", "not-a-snowflake")).Object,
                cancellationToken));
        }

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
            Mock<IDiscordRestInteractionAPI> interactionApi = CreateInteractionApiMock();
            RemoraInteractionResponder responder = new(dispatcher, interactionApi.Object);

            Mock<IInteraction> slashInteraction = CreateSlashInteraction("test");
            await responder.RespondAsync(slashInteraction.Object, cancellationToken);
            Assert.Equal(1, dispatcher.CallCount);
            Assert.Same(slashInteraction.Object, dispatcher.LastInteraction);

            Mock<IInteraction> componentInteraction = CreateInteraction(InteractionType.MessageComponent);
            await responder.RespondAsync(componentInteraction.Object, cancellationToken);
            Assert.Equal(1, dispatcher.CallCount);
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
        public static async Task Responder_DispatchFailure_SendsEphemeralErrorAndDoesNotThrow()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            ThrowingDispatcher dispatcher = new();
            Mock<IDiscordRestInteractionAPI> interactionApi = CreateInteractionApiMock();
            RemoraInteractionResponder responder = new(dispatcher, interactionApi.Object);
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

            return callbackData.Flags.HasValue && callbackData.Flags.Value.HasFlag(MessageFlags.Ephemeral);
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

        private sealed class StubSource : IRemoraSlashCommandSource
        {
            private readonly IRemoraSlashCommand m_command;

            public StubSource(IRemoraSlashCommand command)
            {
                m_command = command;
            }

            public IEnumerable<IRemoraSlashCommand> GetCommands()
            {
                yield return m_command;
            }
        }

        private sealed class RecordingCommand : IRemoraSlashCommand
        {
            public RecordingCommand(string name, IReadOnlyList<RemoraSlashCommandParameter> parameters)
            {
                Name = name;
                Parameters = parameters;
            }

            public string Name { get; }

            public string Description => "test command";

            public IReadOnlyList<RemoraSlashCommandParameter> Parameters { get; }

            public IReadOnlyDictionary<string, object> LastArguments { get; private set; } = new Dictionary<string, object>();

            public bool WasInvoked { get; private set; }

            public Task InvokeAsync(global::Bot.Api.IBotInteractionContext context, IReadOnlyDictionary<string, object> arguments)
            {
                WasInvoked = true;
                LastArguments = new Dictionary<string, object>(arguments);
                return Task.CompletedTask;
            }
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
    }
}
