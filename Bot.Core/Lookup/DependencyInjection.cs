using Bot.Api;
using Microsoft.Extensions.DependencyInjection;

namespace Bot.Core.Lookup
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBotCoreLookupServices(this IServiceCollection services)
        {
            services.AddSingleton<IOfficialUrlProvider, OfficialUrlProvider>();
            services.AddSingleton<IOfficialScriptParser, OfficialScriptParser>();
            services.AddSingleton<ICustomScriptParser, CustomScriptParser>();
            services.AddSingleton<IStringDownloader, StringDownloader>();
            services.AddSingleton<ICustomScriptCache, CustomScriptCache>();
            services.AddSingleton<IOfficialCharacterCache, OfficialCharacterCache>();
            services.AddSingleton<ICharacterStorage, CharacterStorage>();
            services.AddSingleton<ICharacterLookup, CharacterLookup>();
            return services;
        }

        public static IServiceCollection AddBotLookupServices(this IServiceCollection services)
        {
            services.AddSingleton<ILookupEmbedBuilder, LookupEmbedBuilder>();
            services.AddSingleton<IBotLookupService, BotLookupService>();
            return services;
        }
    }
}
