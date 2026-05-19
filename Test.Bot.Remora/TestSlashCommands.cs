using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Bot.Api;
using Bot.Remora;
using Moq;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Rest.Core;
using Xunit;
using BotChannel = Bot.Api.IChannel;
using BotGuild = Bot.Api.IGuild;
using DiscordUser = Remora.Discord.API.Abstractions.Objects.IUser;

namespace Test.Bot.Remora
{
    public class TestSlashCommands
    {
        [Fact]
        public void GameCommands_ExposeExpectedCommandNames()
        {
            string[] commands = GetCommandNames(typeof(GameCommands));

            Assert.Contains("game", commands);
            Assert.Contains("night", commands);
            Assert.Contains("day", commands);
            Assert.Contains("vote", commands);
            Assert.Contains("votetimer", commands);
            Assert.Contains("stopvotetimer", commands);
            Assert.Contains("endgame", commands);
            Assert.Contains("storytellers", commands);
        }

        [Fact]
        public void LookupCommands_ExposeExpectedCommandNames()
        {
            string[] commands = GetCommandNames(typeof(LookupCommands));

            Assert.Contains("lookup", commands);
            Assert.Contains("addscript", commands);
            Assert.Contains("removescript", commands);
            Assert.Contains("listscripts", commands);
            Assert.Contains("refreshscripts", commands);
        }

        [Fact]
        public void MessagingMiscAndSetupCommands_ExposeExpectedCommandNames()
        {
            string[] messaging = GetCommandNames(typeof(MessagingCommands));
            string[] misc = GetCommandNames(typeof(MiscCommands));
            string[] setup = GetCommandNames(typeof(SetupCommands));

            Assert.Contains("evil", messaging);
            Assert.Contains("lunatic", messaging);
            Assert.Contains("announce", misc);
            Assert.Contains("towninfo", setup);
            Assert.Contains("destroytown", setup);
            Assert.Contains("modifytown", setup);
            Assert.Contains("addtown", setup);
            Assert.Contains("removetown", setup);
        }

        [Fact]
        public async Task GameCommands_Game_DelegatesToGameplayHandler()
        {
            Mock<IBotGameplayInteractionHandler> handler = new();
            GameCommands commands = new(handler.Object, CreateInteractionContext().Object, CreateFactory().Object);

            await commands.HandleGameAsync();

            handler.Verify(h => h.CommandGameAsync(It.IsAny<IBotInteractionContext>()), Times.Once);
        }

        [Fact]
        public async Task LookupCommands_AddScript_DelegatesWithArgument()
        {
            Mock<IBotLookupService> lookup = new();
            LookupCommands commands = new(lookup.Object, CreateInteractionContext().Object, CreateFactory().Object);

            await commands.HandleAddScriptAsync("https://example.invalid/script.json");

            lookup.Verify(l => l.AddScriptAsync(It.IsAny<IBotInteractionContext>(), "https://example.invalid/script.json"), Times.Once);
        }

        [Fact]
        public async Task MiscCommands_Announce_DelegatesWithFlag()
        {
            Mock<IAnnouncer> announcer = new();
            MiscCommands commands = new(announcer.Object, CreateInteractionContext().Object, CreateFactory().Object);

            await commands.HandleAnnounceAsync(true);

            announcer.Verify(a => a.CommandSetGuildAnnounce(It.IsAny<IBotInteractionContext>(), true), Times.Once);
        }

        [Fact]
        public async Task SetupCommands_DestroyTown_DelegatesWithTownName()
        {
            Mock<IBotSetup> setup = new();
            SetupCommands commands = new(setup.Object, CreateInteractionContext().Object, CreateFactory().Object);

            await commands.HandleDestroyTownAsync("Ravenswood Bluff");

            setup.Verify(s => s.DestroyTownAsync(It.IsAny<IBotInteractionContext>(), "Ravenswood Bluff"), Times.Once);
        }

        [Fact]
        public async Task MessagingCommands_Lunatic_DelegatesToMessagingService()
        {
            Mock<IBotMessaging> messaging = new();
            MessagingCommands commands = new(messaging.Object, CreateInteractionContext().Object, CreateFactory().Object);

            Mock<DiscordUser> user = new();
            user.SetupGet(u => u.ID).Returns(new Snowflake(1234));
            user.SetupGet(u => u.Username).Returns("tester");

            Mock<IGuildMember> member = new();
            member.SetupGet(m => m.User).Returns(new Optional<DiscordUser>(user.Object));
            member.SetupGet(m => m.Nickname).Returns(default(Optional<string?>));
            member.SetupGet(m => m.Roles).Returns(new List<Snowflake>());

            await commands.HandleLunaticAsync(member.Object, member.Object);

            messaging.Verify(m => m.CommandLunaticMessageAsync(
                It.IsAny<IBotInteractionContext>(),
                It.IsAny<IMember>(),
                It.Is<IReadOnlyCollection<IMember>>(members => members.Count == 1)), Times.Once);
        }

        private static string[] GetCommandNames(Type type)
        {
            return type.GetMethods()
                .Select(m => m.GetCustomAttributes(typeof(CommandAttribute), true).FirstOrDefault())
                .OfType<CommandAttribute>()
                .Select(ReadCommandName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToArray();
        }

        private static string ReadCommandName(CommandAttribute attribute)
        {
            PropertyInfo? textProperty = attribute.GetType().GetProperty("Text", BindingFlags.Instance | BindingFlags.Public);
            if (textProperty is not null)
            {
                string? text = textProperty.GetValue(attribute) as string;
                return text ?? string.Empty;
            }

            PropertyInfo? nameProperty = attribute.GetType().GetProperty("Name", BindingFlags.Instance | BindingFlags.Public);
            if (nameProperty is not null)
            {
                string? name = nameProperty.GetValue(attribute) as string;
                return name ?? string.Empty;
            }

            return string.Empty;
        }

        private static Mock<IInteractionContext> CreateInteractionContext()
        {
            Mock<IInteractionContext> interactionContext = new();
            interactionContext.SetupGet(x => x.Interaction).Returns(new Mock<IInteraction>().Object);
            return interactionContext;
        }

        private static Mock<ILiveRemoraInteractionContextFactory> CreateFactory()
        {
            Mock<BotGuild> guild = new();
            Mock<BotChannel> channel = new();
            Mock<IMember> member = new();
            Mock<IDiscordRestInteractionAPI> interactionApi = new();

            LiveRemoraInteractionContext liveContext = new(
                guild.Object,
                channel.Object,
                member.Object,
                interactionApi.Object,
                new Snowflake(1),
                new Snowflake(2),
                "token",
                CancellationToken.None);

            Mock<ILiveRemoraInteractionContextFactory> factory = new();
            factory
                .Setup(f => f.Create(It.IsAny<IInteraction>(), It.IsAny<CancellationToken>()))
                .Returns(liveContext);
            return factory;
        }
    }
}
