using System.Collections.Generic;
using System.Threading.Tasks;
using Bot.Api;

namespace Bot.Remora
{
    public sealed class RemoraSetupSlashCommands : IRemoraSlashCommandSource
    {
        private readonly IBotSetup m_setup;

        public RemoraSetupSlashCommands(IBotSetup setup)
        {
            m_setup = setup;
        }

        public IEnumerable<IRemoraSlashCommand> GetCommands()
        {
            yield return new CreateTownCommand(m_setup);
            yield return new TownInfoCommand(m_setup);
            yield return new DestroyTownCommand(m_setup);
            yield return new ModifyTownCommand(m_setup);
            yield return new AddTownCommand(m_setup);
            yield return new RemoveTownCommand(m_setup);
        }

        private sealed class CreateTownCommand : IRemoraSlashCommand
        {
            private readonly IBotSetup m_setup;
            public CreateTownCommand(IBotSetup setup) { m_setup = setup; }

            public string Name => "createTown";
            public string Description => "Create a new Town on this server";
            public IReadOnlyList<RemoraSlashCommandParameter> Parameters { get; } = new[]
            {
                new RemoraSlashCommandParameter("townName", "Town Name", RemoraSlashCommandParameterType.String, true),
                new RemoraSlashCommandParameter("playerRole", "Server Player Role - only they can see the town", RemoraSlashCommandParameterType.Role, false),
                new RemoraSlashCommandParameter("storytellerRole", "Server Storyteller Role - only they can see control channels", RemoraSlashCommandParameterType.Role, false),
                new RemoraSlashCommandParameter("useNight", "If true, a Night category full of cottages will be created", RemoraSlashCommandParameterType.Boolean, false),
            };

            public Task InvokeAsync(IBotInteractionContext context, IReadOnlyDictionary<string, object> arguments)
            {
                string townName = arguments.GetRequired<string>("townName");
                IRole? playerRole = arguments.GetOptional<IRole>("playerRole");
                IRole? stRole = arguments.GetOptional<IRole>("storytellerRole");
                bool useNight = arguments.GetBool("useNight", true);
                return m_setup.CreateTownAsync(context, townName, playerRole, stRole, useNight);
            }
        }

        private sealed class TownInfoCommand : IRemoraSlashCommand
        {
            private readonly IBotSetup m_setup;
            public TownInfoCommand(IBotSetup setup) { m_setup = setup; }

            public string Name => "townInfo";
            public string Description => "Get info about any town registered for this server & channel";
            public IReadOnlyList<RemoraSlashCommandParameter> Parameters { get; } = System.Array.Empty<RemoraSlashCommandParameter>();

            public Task InvokeAsync(IBotInteractionContext context, IReadOnlyDictionary<string, object> arguments)
                => m_setup.TownInfoAsync(context);
        }

        private sealed class DestroyTownCommand : IRemoraSlashCommand
        {
            private readonly IBotSetup m_setup;
            public DestroyTownCommand(IBotSetup setup) { m_setup = setup; }

            public string Name => "destroyTown";
            public string Description => "Destroy any channels and roles created by /createtown for the town with the given name";
            public IReadOnlyList<RemoraSlashCommandParameter> Parameters { get; } = new[]
            {
                new RemoraSlashCommandParameter("townName", "Town Name", RemoraSlashCommandParameterType.String, true),
            };

            public Task InvokeAsync(IBotInteractionContext context, IReadOnlyDictionary<string, object> arguments)
                => m_setup.DestroyTownAsync(context, arguments.GetRequired<string>("townName"));
        }

        private sealed class ModifyTownCommand : IRemoraSlashCommand
        {
            private readonly IBotSetup m_setup;
            public ModifyTownCommand(IBotSetup setup) { m_setup = setup; }

            public string Name => "modifyTown";
            public string Description => "Modify one of the optional details of a town";
            public IReadOnlyList<RemoraSlashCommandParameter> Parameters { get; } = new[]
            {
                new RemoraSlashCommandParameter("chatChannel", "Set the (text) chat channel for this town", RemoraSlashCommandParameterType.Channel, false),
                new RemoraSlashCommandParameter("nightCategory", "Set the Night category for this town", RemoraSlashCommandParameterType.Channel, false),
            };

            public Task InvokeAsync(IBotInteractionContext context, IReadOnlyDictionary<string, object> arguments)
            {
                IChannel? chat = arguments.GetOptional<IChannel>("chatChannel");
                IChannelCategory? nightCat = arguments.GetOptional<IChannelCategory>("nightCategory");
                return m_setup.ModifyTownAsync(context, chat, nightCat);
            }
        }

        private sealed class AddTownCommand : IRemoraSlashCommand
        {
            private readonly IBotSetup m_setup;
            public AddTownCommand(IBotSetup setup) { m_setup = setup; }

            public string Name => "addTown";
            public string Description => "Add a new town composed of existing channel and roles on this server";
            public IReadOnlyList<RemoraSlashCommandParameter> Parameters { get; } = new[]
            {
                new RemoraSlashCommandParameter("controlChannel", "Control channel (must be text)", RemoraSlashCommandParameterType.Channel, true),
                new RemoraSlashCommandParameter("townSquare", "Town Square channel (must be voice)", RemoraSlashCommandParameterType.Channel, true),
                new RemoraSlashCommandParameter("dayCategory", "Day Category (must contain control and town square channels)", RemoraSlashCommandParameterType.Channel, true),
                new RemoraSlashCommandParameter("storytellerRole", "Role to grant storytellers in this town during an active game", RemoraSlashCommandParameterType.Role, true),
                new RemoraSlashCommandParameter("villagerRole", "Role to grant villagers in this town during an active game", RemoraSlashCommandParameterType.Role, true),
                new RemoraSlashCommandParameter("nightCategory", "Night Category (optional)", RemoraSlashCommandParameterType.Channel, false),
                new RemoraSlashCommandParameter("chatChannel", "Chat channel (optional, must be text)", RemoraSlashCommandParameterType.Channel, false),
            };

            public Task InvokeAsync(IBotInteractionContext context, IReadOnlyDictionary<string, object> arguments)
            {
                IChannel controlChan = arguments.GetRequired<IChannel>("controlChannel");
                IChannel townSquare = arguments.GetRequired<IChannel>("townSquare");
                IChannelCategory dayCat = arguments.GetRequired<IChannelCategory>("dayCategory");
                IRole stRole = arguments.GetRequired<IRole>("storytellerRole");
                IRole villagerRole = arguments.GetRequired<IRole>("villagerRole");
                IChannelCategory? nightCat = arguments.GetOptional<IChannelCategory>("nightCategory");
                IChannel? chatChan = arguments.GetOptional<IChannel>("chatChannel");
                return m_setup.AddTownAsync(context, controlChan, townSquare, dayCat, nightCat, stRole, villagerRole, chatChan);
            }
        }

        private sealed class RemoveTownCommand : IRemoraSlashCommand
        {
            private readonly IBotSetup m_setup;
            public RemoveTownCommand(IBotSetup setup) { m_setup = setup; }

            public string Name => "removeTown";
            public string Description => "Unregister a town on this server without deleting any channels or roles";
            public IReadOnlyList<RemoraSlashCommandParameter> Parameters { get; } = new[]
            {
                new RemoraSlashCommandParameter("dayCategory", "Town to remove - if blank, must be run from the town's control channel", RemoraSlashCommandParameterType.Channel, false),
            };

            public Task InvokeAsync(IBotInteractionContext context, IReadOnlyDictionary<string, object> arguments)
            {
                IChannelCategory? dayCat = arguments.GetOptional<IChannelCategory>("dayCategory");
                return m_setup.RemoveTownAsync(context, dayCat);
            }
        }
    }
}
