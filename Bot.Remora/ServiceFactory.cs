using Bot.Api;
using Bot.Base;
using System;

namespace Bot.Remora
{
    public static class ServiceFactory
    {
        public static IServiceProvider RegisterServices(IServiceProvider? parentServices)
        {
            ServiceProvider sp = new(parentServices);
            sp.AddService<IColorBuilder>(new RemoraColorBuilder());
            sp.AddService<IRemoraCommandRegistrar>(new NoOpRemoraCommandRegistrar());

            RemoraSlashCommandRegistry registry = new();
            registry.AddSource(spInner => new RemoraGameSlashCommands(spInner.GetService<IBotGameplayInteractionHandler>()));
            registry.AddSource(spInner => new RemoraMessagingSlashCommands(spInner.GetService<IBotMessaging>()));
            registry.AddSource(spInner => new RemoraLookupSlashCommands(spInner.GetService<IBotLookupService>()));
            registry.AddSource(spInner => new RemoraMiscSlashCommands(spInner.GetService<IAnnouncer>()));
            registry.AddSource(spInner => new RemoraSetupSlashCommands(spInner.GetService<IBotSetup>()));
            sp.AddService(registry);
            return sp;
        }
    }
}
