using Bot.Api;
using Bot.Core;
using Moq;
using System;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestRunner : TestBase
    {
        private readonly Mock<IBotClient> m_mockClient = new(MockBehavior.Strict);
        private readonly Mock<IVersionProvider> m_mockVersionProvider = new(MockBehavior.Strict);
        private readonly Mock<IFinalShutdownService> m_mockFinalShutdown = new(MockBehavior.Strict);
        private readonly TaskCompletionSource m_finalShutdownTcs = new();

        public TestRunner()
        {
            m_mockFinalShutdown.SetupGet(fs => fs.ReadyToShutdown).Returns(m_finalShutdownTcs.Task);
            m_mockVersionProvider.Setup(v => v.InitializeVersions());
            m_mockClient.Setup(c => c.ConnectAsync()).Returns(Task.CompletedTask);
            m_mockClient.Setup(c => c.DisconnectAsync()).Returns(Task.CompletedTask);
        }

        [Fact]
        public void ConstructRunner_NoExceptions()
        {
            _ = new BotSystemRunner(m_mockClient.Object, m_mockFinalShutdown.Object, m_mockVersionProvider.Object);
        }

        [Fact]
        public void GiveRunnerSystem_Run_InitializesVersions()
        {
            _ = new BotSystemRunner(m_mockClient.Object, m_mockFinalShutdown.Object, m_mockVersionProvider.Object);
            m_mockVersionProvider.Verify(v => v.InitializeVersions(), Times.Once);
        }

        [Fact]
        public void GiveRunnerSystem_Run_RunsClient()
        {
            BotSystemRunner runner = new(m_mockClient.Object, m_mockFinalShutdown.Object, m_mockVersionProvider.Object);

            m_mockClient.Verify(c => c.ConnectAsync(), Times.Never);

            var t = runner.RunAsync();
            t.Wait(5);

            m_mockClient.Verify(c => c.ConnectAsync(), Times.Once);
        }

        [Fact]
        public void GiveRunnerSystem_RunsForever_NotComplete()
        {
            TaskCompletionSource<bool> tcs = new();
            m_mockClient.Setup(c => c.ConnectAsync()).Returns(tcs.Task);

            BotSystemRunner runner = new(m_mockClient.Object, m_mockFinalShutdown.Object, m_mockVersionProvider.Object);

            var t = runner.RunAsync();
            t.Wait(5);

            Assert.False(t.IsCompleted);
        }

        [Fact]
        public void BotRunner_ShutdownReady_Exits()
        {
            BotSystemRunner runner = new(m_mockClient.Object, m_mockFinalShutdown.Object, m_mockVersionProvider.Object);

            var t = runner.RunAsync();
            t.Wait(5);

            m_mockClient.Verify(c => c.ConnectAsync(), Times.Once);
            m_mockClient.Verify(c => c.DisconnectAsync(), Times.Never);
            Assert.False(t.IsCompleted);

            m_finalShutdownTcs.SetResult();

            t.Wait(5);

            m_mockClient.Verify(c => c.DisconnectAsync(), Times.Once);
            Assert.True(t.IsCompleted);
        }
    }
}
