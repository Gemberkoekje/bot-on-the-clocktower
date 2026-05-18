using Bot.Api;
using Bot.Remora;
using System;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Remora
{
    public class TestServices : TestBase
    {
        [Theory]
        [InlineData(typeof(IColorBuilder), typeof(RemoraColorBuilder))]
        [InlineData(typeof(IRemoraCommandRegistrar), typeof(NoOpRemoraCommandRegistrar))]
        [InlineData(typeof(RemoraSlashCommandRegistry), typeof(RemoraSlashCommandRegistry))]
        public void RegisterServices_CreatesAllRequiredServices(Type serviceInterface, Type serviceImpl)
        {
            var newSp = ServiceFactory.RegisterServices(GetServiceProvider());
            var service = newSp.GetService(serviceInterface);

            Assert.NotNull(service);
            Assert.IsType(serviceImpl, service);
        }
    }
}
