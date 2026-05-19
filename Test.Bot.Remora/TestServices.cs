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
        [Theory]
        [InlineData(typeof(IColorBuilder), typeof(RemoraColorBuilder))]
        [InlineData(typeof(IRemoraCommandRegistrar), typeof(RemoraCommandRegistrar))]
        [InlineData(typeof(RemoraSlashCommandRegistry), typeof(RemoraSlashCommandRegistry))]
        public void RegisterServices_CreatesAllRequiredServices(Type serviceInterface, Type serviceImpl)
        {
            var services = new ServiceCollection();
            var env = new Mock<IEnvironment>();
            env.Setup(e => e.GetEnvironmentVariable("DISCORD_TOKEN")).Returns("token");
            env.Setup(e => e.GetEnvironmentVariable("DEPLOY_TYPE")).Returns("dev");
            services.AddSingleton(env.Object);
            services.AddRemoraServices();

            using var sp = services.BuildServiceProvider();
            var service = sp.GetService(serviceInterface);

            Assert.NotNull(service);
            Assert.IsType(serviceImpl, service);
        }
    }
}
