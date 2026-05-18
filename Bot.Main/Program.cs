namespace Bot.Main
{
    public class Program
    {
        static async Task Main(string[] _)
        {
            // Build configuration
            IConfiguration configuration = BuildConfiguration();
            BotConfiguration botConfig = new();
            configuration.Bind(botConfig);

            // Setup the global Serilog logger instance; if we want more granularity or not to use a single static logger, we can
            // instantiate individual loggers and put them into our services instead
            var logConfig = new LoggerConfiguration()
                .Destructure.ByIgnoringProperties<RemoraMember>(x => x.Roles, x => x.CurrentVoiceChannel)
                .Destructure.ByIgnoringProperties<RemoraRole>(x => x.Mention)
                .Destructure.ByTransforming<RemoraGuild>(x => new { Id = x.Id, Name = x.Name })
                .Destructure.ByIgnoringProperties<RemoraChannel>(x => x.Users)
                .Destructure.ByIgnoringProperties<RemoraChannelCategory>(x => x.Channels)
                .Destructure.ByIgnoringProperties<RemoraInteractionContext>(x => x.ComponentValues)
                .Destructure.ByTransforming<TownRecord>(x => new { GuildId = x.GuildId, ControlChannelId = x.ControlChannelId, ControlChannelName = x.ControlChannel })
                .Destructure.ByTransforming<Town>(x => new { Guild = x.Guild, ControlChannel = x.ControlChannel })
                .Destructure.ByTransforming<Game>(x => new { TownKey = x.TownKey, Storytellers = x.Storytellers, VillagerCount = x.Villagers.Count })
                .WriteTo.File(botConfig.Logging.File.Path, rollingInterval: ParseRollingInterval(botConfig.Logging.File.RollingInterval));

            // Fallback to .env for backward compatibility
            DotEnv.Load(@"..\..\..\..\.env");

            string deployType = botConfig.Deployment.Type;
            if (deployType.Equals("dev"))
            {
                if (botConfig.Logging.Console.Enabled)
                {
                    logConfig = logConfig.WriteTo.Console();
                }
                logConfig = logConfig.MinimumLevel.Debug();
            }
            else
            {
                logConfig = logConfig.MinimumLevel.Information();
            }

            // Apply configured log levels
            if (botConfig.Logging.LogLevel.TryGetValue("Default", out string defaultLevel))
            {
                logConfig = ApplyMinimumLevel(logConfig, defaultLevel);
            }

            Log.Logger = logConfig.CreateLogger();

            LogOutput($"Bot started at {DateTime.UtcNow} using C# runtime {Environment.Version}");

            var program = new Program();
            await program.RunAsync(configuration);

            LogOutput($"Bot stopped at {DateTime.UtcNow}");
            Log.CloseAndFlush();
        }

        private static IConfiguration BuildConfiguration()
        {
            string environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? "Production";

            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddJsonFile("usersettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<Program>(optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

        private static RollingInterval ParseRollingInterval(string interval)
        {
            return interval switch
            {
                "Day" => RollingInterval.Day,
                "Hour" => RollingInterval.Hour,
                "Month" => RollingInterval.Month,
                "Year" => RollingInterval.Year,
                _ => RollingInterval.Day
            };
        }

        private static LoggerConfiguration ApplyMinimumLevel(LoggerConfiguration config, string level)
        {
            return level switch
            {
                "Debug" => config.MinimumLevel.Debug(),
                "Information" => config.MinimumLevel.Information(),
                "Warning" => config.MinimumLevel.Warning(),
                "Error" => config.MinimumLevel.Error(),
                _ => config
            };
        }

        private async Task RunAsync(IConfiguration configuration, CancellationToken ct)
        {
            var services = new ServiceCollection();
            services.AddBotBaseServices(configuration);
            services.AddBotDatabaseServices();
            services.AddBotCoreServices(ct);
            services.AddBotGameplayServices();
            services.AddRemoraServices();

            using var sp = services.BuildServiceProvider();

            var runner = sp.GetRequiredService<BotSystemRunner>();
            await runner.RunAsync();
        }

        public static IServiceProvider RegisterServices(IConfiguration configuration)
        {
            var services = new ServiceCollection();
            services.AddBotBaseServices(configuration);
            return services.BuildServiceProvider();
        }

        public async Task RunAsync(IConfiguration configuration)
        {
            using (var cts = new CancellationTokenSource())
            {
                await RunAsync(configuration, cts);
            }
        }

        private async Task RunAsync(IConfiguration configuration, CancellationTokenSource cts)
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            ConsoleCancelEventHandler cancelCb = (s, e) => Console_CancelKeyPress(s, e, cts);
            Console.CancelKeyPress += cancelCb;

            await RunAsync(configuration, cts.Token);

            Console.CancelKeyPress -= cancelCb;
            AppDomain.CurrentDomain.ProcessExit -= CurrentDomain_ProcessExit;
        }

        private void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            LogOutput($"ProcessExit at {DateTime.UtcNow}");
        }

        private static void Console_CancelKeyPress(object? _, ConsoleCancelEventArgs e, CancellationTokenSource cts)
        {
            e.Cancel = true;
            if (!cts.IsCancellationRequested)
            {
                LogOutput($"Application cancellation requested at {DateTime.UtcNow}");
                cts.Cancel();
            }
        }

        private static void LogOutput(string s)
        {
            // Until we figure out what's going on with shutdown, we're Console logging things in addition to Serilog logging things
            Console.WriteLine(s);
            Log.Information(s);
        }
    }
}
