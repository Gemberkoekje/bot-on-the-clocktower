using Bot.Api;
using Bot.Core.Lookup;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core.Lookup
{
    public class TestLookupServiceRegistration : TestBase
    {
        [Theory]
        [InlineData(typeof(ICustomScriptParser), typeof(CustomScriptParser))]
        [InlineData(typeof(IStringDownloader), typeof(StringDownloader))]
        [InlineData(typeof(ICharacterStorage), typeof(CharacterStorage))]
        [InlineData(typeof(ICharacterLookup), typeof(CharacterLookup))]
        [InlineData(typeof(IOfficialCharacterCache), typeof(OfficialCharacterCache))]
        [InlineData(typeof(ICustomScriptCache), typeof(CustomScriptCache))]
        [InlineData(typeof(IOfficialUrlProvider), typeof(OfficialUrlProvider))]
        [InlineData(typeof(IOfficialScriptParser), typeof(OfficialScriptParser))]
        public void RegisterLookupServices_RegistersAllRequiredCoreServices(Type serviceInterface, Type serviceImpl)
        {
            var services = new ServiceCollection();
            services.AddBotCoreLookupServices();

            AssertRegistered(services, serviceInterface, serviceImpl);
        }

        [Theory]
        [InlineData(typeof(ILookupEmbedBuilder), typeof(LookupEmbedBuilder))]
        [InlineData(typeof(IBotLookupService), typeof(BotLookupService))]
        public void RegisterLookupServices_RegistersAllRequiredBotServices(Type serviceInterface, Type serviceImpl)
        {
            var services = new ServiceCollection();
            services.AddBotLookupServices();

            AssertRegistered(services, serviceInterface, serviceImpl);
        }

        private static void AssertRegistered(IServiceCollection services, Type serviceInterface, Type serviceImpl)
        {
            var descriptor = services.FirstOrDefault(d => d.ServiceType == serviceInterface);
            Assert.NotNull(descriptor);
            Assert.True(
                descriptor!.ImplementationType == serviceImpl || descriptor.ImplementationFactory != null,
                $"Expected {serviceInterface.Name} to be registered as {serviceImpl.Name} (directly or via factory).");
        }
    }
}
