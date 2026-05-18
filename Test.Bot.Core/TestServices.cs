using Bot.Api;
using Bot.Core;
using Bot.Core.Callbacks;
using Bot.Core.Interaction;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestServices : TestBase
    {
        [Theory]
        [InlineData(typeof(IGuildInteractionErrorHandler), typeof(GuildInteractionErrorHandler))]
        [InlineData(typeof(ITownInteractionErrorHandler), typeof(TownInteractionErrorHandler))]
        [InlineData(typeof(ICallbackSchedulerFactory), typeof(CallbackSchedulerFactory))]
        [InlineData(typeof(IProcessLoggerFactory), typeof(ProcessLoggerFactory))]
        [InlineData(typeof(IComponentService), typeof(ComponentService))]
        [InlineData(typeof(IShuffleService), typeof(ShuffleService))]
        [InlineData(typeof(IFinalShutdownService), typeof(ShutdownService))]
        [InlineData(typeof(IShutdownPreventionService), typeof(ShutdownService))]
        public void RegisterCoreServices_RegistersAllRequiredServices(Type serviceInterface, Type serviceImpl)
        {
            var services = new ServiceCollection();
            services.AddBotCoreServices(CancellationToken.None);

            AssertRegistered(services, serviceInterface, serviceImpl);
        }

        [Theory]
        [InlineData(typeof(IVoteHandler), typeof(BotGameplay))]
        [InlineData(typeof(IBotGameplayInteractionHandler), typeof(BotGameplayInteractionHandler))]
        [InlineData(typeof(IBotMessaging), typeof(BotMessaging))]
        [InlineData(typeof(IGuildInteractionQueue), typeof(GuildInteractionQueue))]
        [InlineData(typeof(ITownInteractionQueue), typeof(TownInteractionQueue))]
        [InlineData(typeof(IGuildInteractionWrapper), typeof(GuildInteractionWrapper))]
        [InlineData(typeof(ITownInteractionWrapper), typeof(TownInteractionWrapper))]
        [InlineData(typeof(ITownCleanup), typeof(TownCleanup))]
        [InlineData(typeof(ITownResolver), typeof(TownResolver))]
        [InlineData(typeof(ILegacyCommandReminder), typeof(LegacyCommandReminder))]
        public void CreateBotServices_RegistersAllRequiredServices(Type serviceInterfaceType, Type serviceImplType)
        {
            var services = new ServiceCollection();
            services.AddBotGameplayServices();

            AssertRegistered(services, serviceInterfaceType, serviceImplType);
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
