using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bot.Api;
using Bot.Remora;
using Moq;
using Xunit;

namespace Test.Bot.Remora
{
    public class TestSlashCommands
    {
        [Fact]
        public void GameSource_ContainsAllExpectedCommands()
        {
            var handler = new Mock<IBotGameplayInteractionHandler>();
            var source = new RemoraGameSlashCommands(handler.Object);
            var names = source.GetCommands().Select(c => c.Name).ToArray();

            Assert.Contains("game", names);
            Assert.Contains("night", names);
            Assert.Contains("day", names);
            Assert.Contains("vote", names);
            Assert.Contains("voteTimer", names);
            Assert.Contains("stopVoteTimer", names);
            Assert.Contains("endGame", names);
            Assert.Contains("storytellers", names);
        }

        [Fact]
        public async Task GameSource_GameCommand_DelegatesToHandler()
        {
            var handler = new Mock<IBotGameplayInteractionHandler>();
            var source = new RemoraGameSlashCommands(handler.Object);
            var game = source.GetCommands().First(c => c.Name == "game");

            var ctx = new Mock<IBotInteractionContext>().Object;
            await game.InvokeAsync(ctx, new Dictionary<string, object>());

            handler.Verify(h => h.CommandGameAsync(ctx), Times.Once);
        }

        [Fact]
        public async Task GameSource_VoteTimer_PassesTimeString()
        {
            var handler = new Mock<IBotGameplayInteractionHandler>();
            var source = new RemoraGameSlashCommands(handler.Object);
            var cmd = source.GetCommands().First(c => c.Name == "voteTimer");

            var ctx = new Mock<IBotInteractionContext>().Object;
            await cmd.InvokeAsync(ctx, new Dictionary<string, object> { ["timeString"] = "5m" });

            handler.Verify(h => h.RunVoteTimerAsync(ctx, "5m"), Times.Once);
        }

        [Fact]
        public async Task GameSource_Storytellers_FiltersNullsAndPassesMembers()
        {
            var handler = new Mock<IBotGameplayInteractionHandler>();
            var source = new RemoraGameSlashCommands(handler.Object);
            var cmd = source.GetCommands().First(c => c.Name == "storytellers");

            var ctx = new Mock<IBotInteractionContext>().Object;
            var m1 = new Mock<IMember>().Object;
            var m2 = new Mock<IMember>().Object;
            await cmd.InvokeAsync(ctx, new Dictionary<string, object>
            {
                ["user1"] = m1,
                ["user3"] = m2,
            });

            handler.Verify(h => h.CommandSetStorytellersAsync(ctx, It.Is<IEnumerable<IMember>>(
                e => e.SequenceEqual(new[] { m1, m2 }))), Times.Once);
        }

        [Fact]
        public void LookupSource_ContainsAllExpectedCommands()
        {
            var lookup = new Mock<IBotLookupService>();
            var source = new RemoraLookupSlashCommands(lookup.Object);
            var names = source.GetCommands().Select(c => c.Name).ToArray();

            Assert.Contains("lookup", names);
            Assert.Contains("addScript", names);
            Assert.Contains("removeScript", names);
            Assert.Contains("listScripts", names);
            Assert.Contains("refreshScripts", names);
        }

        [Fact]
        public async Task LookupSource_AddScript_RoutesArgument()
        {
            var lookup = new Mock<IBotLookupService>();
            var source = new RemoraLookupSlashCommands(lookup.Object);
            var cmd = source.GetCommands().First(c => c.Name == "addScript");

            var ctx = new Mock<IBotInteractionContext>().Object;
            await cmd.InvokeAsync(ctx, new Dictionary<string, object> { ["scriptJsonUrl"] = "https://x" });

            lookup.Verify(l => l.AddScriptAsync(ctx, "https://x"), Times.Once);
        }

        [Fact]
        public void MessagingSource_ContainsEvilAndLunatic()
        {
            var msg = new Mock<IBotMessaging>();
            var source = new RemoraMessagingSlashCommands(msg.Object);
            var names = source.GetCommands().Select(c => c.Name).ToArray();

            Assert.Contains("evil", names);
            Assert.Contains("lunatic", names);
        }

        [Fact]
        public async Task MessagingSource_Evil_PassesRequiredMembers()
        {
            var msg = new Mock<IBotMessaging>();
            var source = new RemoraMessagingSlashCommands(msg.Object);
            var cmd = source.GetCommands().First(c => c.Name == "evil");

            var ctx = new Mock<IBotInteractionContext>().Object;
            var demon = new Mock<IMember>().Object;
            var minion = new Mock<IMember>().Object;
            await cmd.InvokeAsync(ctx, new Dictionary<string, object>
            {
                ["demon"] = demon,
                ["minion1"] = minion,
            });

            msg.Verify(m => m.CommandEvilMessageAsync(ctx, demon,
                It.Is<IReadOnlyCollection<IMember>>(c => c.SequenceEqual(new[] { minion })),
                null), Times.Once);
        }

        [Fact]
        public void MiscSource_ContainsAnnounce()
        {
            var ann = new Mock<IAnnouncer>();
            var source = new RemoraMiscSlashCommands(ann.Object);
            Assert.Contains("announce", source.GetCommands().Select(c => c.Name));
        }

        [Fact]
        public async Task MiscSource_Announce_PassesHearFlag()
        {
            var ann = new Mock<IAnnouncer>();
            var source = new RemoraMiscSlashCommands(ann.Object);
            var cmd = source.GetCommands().First();

            var ctx = new Mock<IBotInteractionContext>().Object;
            await cmd.InvokeAsync(ctx, new Dictionary<string, object> { ["hearAnnouncements"] = true });

            ann.Verify(a => a.CommandSetGuildAnnounce(ctx, true), Times.Once);
        }

        [Fact]
        public void SetupSource_ContainsAllExpectedCommands()
        {
            var setup = new Mock<IBotSetup>();
            var source = new RemoraSetupSlashCommands(setup.Object);
            var names = source.GetCommands().Select(c => c.Name).ToArray();

            Assert.Contains("createTown", names);
            Assert.Contains("townInfo", names);
            Assert.Contains("destroyTown", names);
            Assert.Contains("modifyTown", names);
            Assert.Contains("addTown", names);
            Assert.Contains("removeTown", names);
        }

        [Fact]
        public async Task SetupSource_CreateTown_RoutesArguments()
        {
            var setup = new Mock<IBotSetup>();
            var source = new RemoraSetupSlashCommands(setup.Object);
            var cmd = source.GetCommands().First(c => c.Name == "createTown");

            var ctx = new Mock<IBotInteractionContext>().Object;
            await cmd.InvokeAsync(ctx, new Dictionary<string, object>
            {
                ["townName"] = "MyTown",
                ["useNight"] = false,
            });

            setup.Verify(s => s.CreateTownAsync(ctx, "MyTown", null, null, false), Times.Once);
        }

        [Fact]
        public void ServiceFactory_RegistersAllSlashCommandSources()
        {
            var sp = ServiceFactory.RegisterServices(null);
            var registry = sp.GetService(typeof(RemoraSlashCommandRegistry)) as RemoraSlashCommandRegistry;

            Assert.NotNull(registry);
            Assert.Contains(typeof(RemoraGameSlashCommands), registry!.SourceTypes);
            Assert.Contains(typeof(RemoraLookupSlashCommands), registry.SourceTypes);
            Assert.Contains(typeof(RemoraMessagingSlashCommands), registry.SourceTypes);
            Assert.Contains(typeof(RemoraMiscSlashCommands), registry.SourceTypes);
            Assert.Contains(typeof(RemoraSetupSlashCommands), registry.SourceTypes);
        }

        [Fact]
        public void Registry_ResolveCommands_CreatesCommandsViaFactoryInjection()
        {
            var registry = new RemoraSlashCommandRegistry();
            var services = new global::Bot.Base.ServiceProvider();

            var gameplay = new Mock<IBotGameplayInteractionHandler>();
            services.AddService(gameplay.Object);
            registry.AddSource(sp => new RemoraGameSlashCommands((IBotGameplayInteractionHandler)sp.GetService(typeof(IBotGameplayInteractionHandler))!));

            var commands = registry.ResolveCommands(services).Select(c => c.Name).ToArray();

            Assert.Contains("game", commands);
            Assert.Contains("storytellers", commands);
        }
    }
}
