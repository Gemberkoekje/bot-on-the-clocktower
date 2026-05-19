using Bot.Api;
using Bot.Remora;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Remora
{
    public class TestClient : TestBase
    {
        [Fact]
        public void ClientConnect_NoDiscordToken_ThrowsException()
        {
            var env = new Mock<IEnvironment>();
            Assert.Throws<RemoraClient.InvalidDiscordTokenException>(() => new RemoraClient(env.Object));
        }

        [Fact]
        public void ClientConnect_InvalidDeployType_ThrowsException()
        {
            var env = RegisterMock(new Mock<IEnvironment>());
            env.Setup(e => e.GetEnvironmentVariable("DISCORD_TOKEN")).Returns("token");
            env.Setup(e => e.GetEnvironmentVariable("DEPLOY_TYPE")).Returns("staging");

            Assert.Throws<RemoraClient.InvalidDeployTypeException>(() => new RemoraClient(env.Object));
        }

        [Theory]
        [InlineData("dev")]
        [InlineData("prod")]
        public void ClientConstruct_ValidDeployType_DoesNotThrow(string deployType)
        {
            var env = RegisterMock(new Mock<IEnvironment>());
            env.Setup(e => e.GetEnvironmentVariable("DISCORD_TOKEN")).Returns("token");
            env.Setup(e => e.GetEnvironmentVariable("DEPLOY_TYPE")).Returns(deployType);

            _ = new RemoraClient(env.Object);
        }

        [Fact]
        public static async Task ConnectAsync_RaisesConnectedOnce()
        {
            ServiceProvider sp = new();
            Mock<IEnvironment> env = new();
            env.Setup(e => e.GetEnvironmentVariable("DISCORD_TOKEN")).Returns("token");
            env.Setup(e => e.GetEnvironmentVariable("DEPLOY_TYPE")).Returns("dev");
            sp.AddService(env.Object);

            RemoraClient client = new(env.Object);
            int connectedCalls = 0;
            client.Connected += (_, _) => connectedCalls++;

            await client.ConnectAsync();
            await client.ConnectAsync();

            Assert.Equal(1, connectedCalls);
            Assert.True(client.IsConnected);
        }

        [Fact]
        public static async Task GetGuildAsync_ReturnsRegisteredGuild()
        {
            ServiceProvider sp = new();
            Mock<IEnvironment> env = new();
            env.Setup(e => e.GetEnvironmentVariable("DISCORD_TOKEN")).Returns("token");
            env.Setup(e => e.GetEnvironmentVariable("DEPLOY_TYPE")).Returns("prod");
            sp.AddService(env.Object);

            RemoraClient client = new(env.Object);
            RemoraGuild guild = new(123, "guild");
            client.RegisterGuild(guild);

            IGuild? retrieved = await client.GetGuildAsync(123);

            Assert.Same(guild, retrieved);
        }

        [Fact]
        public static void RaiseMessageCreated_BotMessage_NotRaised()
        {
            ServiceProvider sp = new();
            Mock<IEnvironment> env = new();
            env.Setup(e => e.GetEnvironmentVariable("DISCORD_TOKEN")).Returns("token");
            env.Setup(e => e.GetEnvironmentVariable("DEPLOY_TYPE")).Returns("prod");
            sp.AddService(env.Object);

            RemoraClient client = new(env.Object);
            int messageCalls = 0;
            client.MessageCreated += (_, _) => messageCalls++;

            RemoraChannel channel = new(1, "general");
            client.RaiseMessageCreated(channel, "ignored", true);

            Assert.Equal(0, messageCalls);
        }

        [Fact]
        public static void RaiseMessageCreated_UserMessage_Raised()
        {
            ServiceProvider sp = new();
            Mock<IEnvironment> env = new();
            env.Setup(e => e.GetEnvironmentVariable("DISCORD_TOKEN")).Returns("token");
            env.Setup(e => e.GetEnvironmentVariable("DEPLOY_TYPE")).Returns("dev");
            sp.AddService(env.Object);

            RemoraClient client = new(env.Object);
            int messageCalls = 0;
            client.MessageCreated += (_, args) =>
            {
                messageCalls++;
                Assert.Equal("hello", args.Message);
            };

            RemoraChannel channel = new(1, "general");
            client.RaiseMessageCreated(channel, "hello", false);

            Assert.Equal(1, messageCalls);
        }

        [Fact]
        public static async Task DispatchComponentInteractionAsync_WithoutComponentService_ReturnsFalse()
        {
            ServiceProvider sp = new();
            Mock<IEnvironment> env = new();
            env.Setup(e => e.GetEnvironmentVariable("DISCORD_TOKEN")).Returns("token");
            env.Setup(e => e.GetEnvironmentVariable("DEPLOY_TYPE")).Returns("dev");
            sp.AddService(env.Object);

            RemoraClient client = new(env.Object);
            RemoraGuild guild = new(1, "guild");
            RemoraChannel channel = new(2, "general");
            RemoraMember member = new(3, "member");

            bool result = await client.DispatchComponentInteractionAsync(guild, channel, member, "component-id", Array.Empty<string>());

            Assert.False(result);
        }

        [Fact]
        public static async Task DispatchComponentInteractionAsync_WithComponentService_UsesContextAndReturnsValue()
        {
            ServiceProvider sp = new();
            Mock<IEnvironment> env = new();
            env.Setup(e => e.GetEnvironmentVariable("DISCORD_TOKEN")).Returns("token");
            env.Setup(e => e.GetEnvironmentVariable("DEPLOY_TYPE")).Returns("prod");
            sp.AddService(env.Object);

            Mock<IComponentService> componentService = new();
            IBotInteractionContext? seenContext = null;
            componentService
                .Setup(c => c.CallAsync(It.IsAny<IBotInteractionContext>()))
                .Callback<IBotInteractionContext>(ctx => seenContext = ctx)
                .ReturnsAsync(true);
            sp.AddService(componentService.Object);

            RemoraClient client = new(env.Object, componentService: componentService.Object);
            RemoraGuild guild = new(11, "guild");
            RemoraChannel channel = new(22, "general");
            RemoraMember member = new(33, "member");

            bool result = await client.DispatchComponentInteractionAsync(guild, channel, member, "my-component", new[] { "a", "b" });

            Assert.True(result);
            Assert.NotNull(seenContext);
            Assert.Equal("my-component", seenContext!.ComponentCustomId);
            Assert.Equal(new[] { "a", "b" }, seenContext.ComponentValues);
            Assert.Same(guild, seenContext.Guild);
            Assert.Same(channel, seenContext.Channel);
            Assert.Same(member, seenContext.Member);
        }

        [Fact]
        public static void CommandPlan_Dev_UsesGuildRegistration_NoGlobal()
        {
            ServiceProvider sp = new();
            Mock<IEnvironment> env = new();
            env.Setup(e => e.GetEnvironmentVariable("DISCORD_TOKEN")).Returns("token");
            env.Setup(e => e.GetEnvironmentVariable("DEPLOY_TYPE")).Returns("dev");
            sp.AddService(env.Object);

            RemoraClient client = new(env.Object);

            Assert.Equal("dev", client.CommandRegistrationPlan.DeployType);
            Assert.False(client.CommandRegistrationPlan.RegisterGlobalCommands);
            Assert.False(client.CommandRegistrationPlan.ClearDevGuildCommands);
            Assert.NotEmpty(client.CommandRegistrationPlan.DevGuildIds);
        }

        [Fact]
        public static async Task CommandPlan_Prod_UsesGlobalAndClearsDevGuilds()
        {
            ServiceProvider sp = new();
            Mock<IEnvironment> env = new();
            env.Setup(e => e.GetEnvironmentVariable("DISCORD_TOKEN")).Returns("token");
            env.Setup(e => e.GetEnvironmentVariable("DEPLOY_TYPE")).Returns("prod");
            sp.AddService(env.Object);

            RemoraClient client = new(env.Object);

            Assert.Equal("prod", client.CommandRegistrationPlan.DeployType);
            Assert.True(client.CommandRegistrationPlan.RegisterGlobalCommands);
            Assert.True(client.CommandRegistrationPlan.ClearDevGuildCommands);
            Assert.NotEmpty(client.CommandRegistrationPlan.DevGuildIds);

            await client.ConnectAsync();

            Assert.True(client.IsConnected);
        }

        [Fact]
        public static void CommandPlan_CsvGuildIds_PreferredOverDefaults()
        {
            ServiceProvider sp = new();
            Mock<IEnvironment> env = new();
            env.Setup(e => e.GetEnvironmentVariable("DISCORD_TOKEN")).Returns("token");
            env.Setup(e => e.GetEnvironmentVariable("DEPLOY_TYPE")).Returns("dev");
            env.Setup(e => e.GetEnvironmentVariable("DISCORD_DEV_GUILD_IDS")).Returns("42, 43,invalid,44");
            sp.AddService(env.Object);

            RemoraClient client = new(env.Object);

            Assert.Equal(new ulong[] { 42, 43, 44 }, client.CommandRegistrationPlan.DevGuildIds.ToArray());
        }

        [Fact]
        public static void CommandPlan_IndexedGuildIds_UsedWhenCsvMissing()
        {
            ServiceProvider sp = new();
            Mock<IEnvironment> env = new();
            env.Setup(e => e.GetEnvironmentVariable("DISCORD_TOKEN")).Returns("token");
            env.Setup(e => e.GetEnvironmentVariable("DEPLOY_TYPE")).Returns("prod");
            env.Setup(e => e.GetEnvironmentVariable("DISCORD_DEV_GUILD_IDS")).Returns((string?)null);
            env.Setup(e => e.GetEnvironmentVariable("Discord:DevGuildIds:0")).Returns("1001");
            env.Setup(e => e.GetEnvironmentVariable("Discord:DevGuildIds:1")).Returns("1002");
            env.Setup(e => e.GetEnvironmentVariable("Discord:DevGuildIds:2")).Returns("not-a-number");
            sp.AddService(env.Object);

            RemoraClient client = new(env.Object);

            Assert.Equal(new ulong[] { 1001, 1002 }, client.CommandRegistrationPlan.DevGuildIds.ToArray());
        }

    }
}
