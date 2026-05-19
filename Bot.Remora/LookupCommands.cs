using System.ComponentModel;
using System.Threading.Tasks;
using Bot.Api;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Bot.Remora
{
    public sealed class LookupCommands : CommandGroup
    {
        private readonly IBotLookupService m_lookup;
        private readonly IInteractionContext m_interactionContext;
        private readonly ILiveRemoraInteractionContextFactory m_contextFactory;

        public LookupCommands(
            IBotLookupService lookup,
            IInteractionContext interactionContext,
            ILiveRemoraInteractionContextFactory contextFactory)
        {
            m_lookup = lookup;
            m_interactionContext = interactionContext;
            m_contextFactory = contextFactory;
        }

        [Command("lookup")]
        [Description("Look up a character")]
        public async Task<IResult> HandleLookupAsync([Description("String you want to look up")] string lookupString)
        {
            await m_lookup.LookupAsync(CreateContext(), lookupString);
            return Result.FromSuccess();
        }

        [Command("addscript")]
        [Description("Add a script json URL for later lookup")]
        public async Task<IResult> HandleAddScriptAsync([Description("URL pointing at a json file for the script")] string scriptJsonUrl)
        {
            await m_lookup.AddScriptAsync(CreateContext(), scriptJsonUrl);
            return Result.FromSuccess();
        }

        [Command("removescript")]
        [Description("Remove script json URL previously added")]
        public async Task<IResult> HandleRemoveScriptAsync([Description("URL pointing at a json file for the script")] string scriptJsonUrl)
        {
            await m_lookup.RemoveScriptAsync(CreateContext(), scriptJsonUrl);
            return Result.FromSuccess();
        }

        [Command("listscripts")]
        [Description("List script json URLs added to this server")]
        public async Task<IResult> HandleListScriptsAsync()
        {
            await m_lookup.ListScriptsAsync(CreateContext());
            return Result.FromSuccess();
        }

        [Command("refreshscripts")]
        [Description("Refresh scripts registered for this server")]
        public async Task<IResult> HandleRefreshScriptsAsync()
        {
            await m_lookup.RefreshScriptsAsync(CreateContext());
            return Result.FromSuccess();
        }

        private IBotInteractionContext CreateContext()
        {
            return m_contextFactory.Create(m_interactionContext.Interaction);
        }
    }
}
