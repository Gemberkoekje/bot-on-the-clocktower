using Bot.Api;
using Bot.Base;
using Bot.Core;
using Bot.Database;
using Bot.Remora;
using Destructurama;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bot.Main
{
    public class Program
    {
        static async Task Main(string[] _)
        {
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
                .WriteTo.File("logs/botc.log", rollingInterval: RollingInterval.Day);

            DotEnv.Load(@"..\..\..\..\.env");

            string deployType = Environment.GetEnvironmentVariable("DEPLOY_TYPE") ?? "prod";
            if(deployType.Equals("dev"))
            {
                logConfig = logConfig.WriteTo.Console();
                logConfig = logConfig.MinimumLevel.Debug();
            }
            else
            {
                logConfig = logConfig.MinimumLevel.Information();
            }
            Log.Logger = logConfig.CreateLogger();

            LogOutput($"Bot started at {DateTime.UtcNow} using C# runtime {Environment.Version}");

            var program = new Program();
            await program.RunAsync();

            LogOutput($"Bot stopped at {DateTime.UtcNow}");
            Log.CloseAndFlush();
        }

        private async Task RunAsync(CancellationToken ct)
        {
            var sp = RegisterServices();
            sp = Database.ServiceFactory.RegisterServices(sp);

            DatabaseFactory dbp = new(sp);
            sp = dbp.Connect();

            sp = Core.ServiceFactory.RegisterCoreServices(sp, ct);
            sp = Core.Lookup.LookupServiceFactory.RegisterCoreLookupServices(sp);

            sp = Remora.ServiceFactory.RegisterServices(sp);

            var runner = new BotSystemRunner(sp, new RemoraSystem());
            await runner.RunAsync();
        }

        public static IServiceProvider RegisterServices()
        {
            var sp = new ServiceProvider();
            sp.AddService(Log.Logger);
            sp.AddService<IDateTime>(new DateTimeStatic());
            sp.AddService<IEnvironment>(new ProgramEnvironment());
            sp.AddService<ITask>(new TaskStatic());
            return sp;
        }

        public async Task RunAsync()
        {
            using (var cts = new CancellationTokenSource())
            {
                await RunAsync(cts);
            }
        }

        private async Task RunAsync(CancellationTokenSource cts)
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            ConsoleCancelEventHandler cancelCb = (s, e) => Console_CancelKeyPress(s, e, cts);
            Console.CancelKeyPress += cancelCb;

            await RunAsync(cts.Token);

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
