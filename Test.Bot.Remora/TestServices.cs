using Bot.Api;
using Bot.Remora;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Remora
{
    public class TestServices : TestBase
    {
        private const string DiscordToken = "token";
        private const string DeployTypeDev = "dev";

        [Theory]
        [InlineData(typeof(IColorBuilder), typeof(RemoraColorBuilder))]
        [InlineData(typeof(ILiveRemoraInteractionContextFactory), typeof(LiveRemoraInteractionContextFactory))]
        public void RegisterServices_CreatesAllRequiredServices(Type serviceInterface, Type serviceImpl)
        {
            var services = new ServiceCollection();
            var env = new Mock<IEnvironment>();
            env.Setup(e => e.GetEnvironmentVariable("DISCORD_TOKEN")).Returns(DiscordToken);
            env.Setup(e => e.GetEnvironmentVariable("DEPLOY_TYPE")).Returns(DeployTypeDev);
            services.AddSingleton(env.Object);
            services.AddSingleton(new Mock<IComponentService>().Object);
            services.AddRemoraServices();

            using var sp = services.BuildServiceProvider();
            var service = sp.GetService(serviceInterface);

            Assert.NotNull(service);
            Assert.IsType(serviceImpl, service);
        }

    }
}
