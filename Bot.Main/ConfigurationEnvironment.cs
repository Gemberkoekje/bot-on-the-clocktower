namespace Bot.Main
{
    public class ConfigurationEnvironment : IEnvironment
    {
        private readonly IConfiguration m_configuration;
        private readonly BotConfiguration m_botConfig;

        public ConfigurationEnvironment(IConfiguration configuration)
        {
            m_configuration = configuration;
            m_botConfig = new BotConfiguration();
            m_configuration.Bind(m_botConfig);
        }

        public string GetEnvironmentVariable(string name)
        {
            string value = name switch
            {
                "DISCORD_TOKEN" => m_botConfig.Discord.Token,
                "DISCORD_DEV_GUILD_IDS" => string.Join(",", m_botConfig.Discord.DevGuildIds),
                "DEPLOY_TYPE" => m_botConfig.Deployment.Type,
                _ => m_configuration[name] ?? string.Empty,
            };

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return Environment.GetEnvironmentVariable(name) ?? string.Empty;
        }

        public BotConfiguration GetBotConfiguration() => m_botConfig;
    }
}
