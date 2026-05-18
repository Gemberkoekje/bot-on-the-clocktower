namespace Bot.Main
{
    public class BotConfiguration
    {
        public LoggingConfiguration Logging { get; set; } = new();
        public DiscordConfiguration Discord { get; set; } = new();
        public DeploymentConfiguration Deployment { get; set; } = new();
    }

    public class LoggingConfiguration
    {
        public Dictionary<string, string> LogLevel { get; set; } = new();
        public FileLoggingConfiguration File { get; set; } = new();
        public ConsoleLoggingConfiguration Console { get; set; } = new();
    }

    public class FileLoggingConfiguration
    {
        public string Path { get; set; } = "logs/botc.log";
        public string RollingInterval { get; set; } = "Day";
    }

    public class ConsoleLoggingConfiguration
    {
        public bool Enabled { get; set; }
    }

    public class DiscordConfiguration
    {
        public string Token { get; set; } = string.Empty;
        public List<ulong> DevGuildIds { get; set; } = new();
    }

    public class DeploymentConfiguration
    {
        public string Type { get; set; } = "prod";
    }
}
