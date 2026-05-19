using System;
using System.Collections.Generic;
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
            Mock<IApplicationCommandData> data = new();
            data.SetupGet(d => d.Name).Returns(name);
            data.SetupGet(d => d.Options).Returns(new Optional<IReadOnlyList<IApplicationCommandInteractionDataOption>>(options));

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
