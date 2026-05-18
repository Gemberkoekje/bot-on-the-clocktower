using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bot.Api;

namespace Bot.Remora
{
    public sealed class RemoraLookupSlashCommands : IRemoraSlashCommandSource
    {
        private readonly IBotLookupService m_lookup;

        public RemoraLookupSlashCommands(IBotLookupService lookup)
        {
            m_lookup = lookup;
        }

        public IEnumerable<IRemoraSlashCommand> GetCommands()
        {
            yield return new StringCommand("lookup", "Look up a character", "lookupString", "String you want to look up", m_lookup.LookupAsync);
            yield return new StringCommand("addScript", "Add a script json URL for later lookup", "scriptJsonUrl", "URL pointing at a json file for the script", m_lookup.AddScriptAsync);
            yield return new StringCommand("removeScript", "Remove script json URL previously added", "scriptJsonUrl", "URL pointing at a json file for the script", m_lookup.RemoveScriptAsync);
            yield return new NoArgCommand("listScripts", "List script json URLs added to this server", m_lookup.ListScriptsAsync);
            yield return new NoArgCommand("refreshScripts", "Refresh scripts registered for this server", m_lookup.RefreshScriptsAsync);
        }

        private sealed class StringCommand : IRemoraSlashCommand
        {
            private readonly string m_paramName;
            private readonly Func<IBotInteractionContext, string, Task> m_callback;

            public StringCommand(string name, string description, string paramName, string paramDescription, Func<IBotInteractionContext, string, Task> callback)
            {
                Name = name;
                Description = description;
                m_paramName = paramName;
                m_callback = callback;
                Parameters = new[] { new RemoraSlashCommandParameter(paramName, paramDescription, RemoraSlashCommandParameterType.String, true) };
            }

            public string Name { get; }
            public string Description { get; }
            public IReadOnlyList<RemoraSlashCommandParameter> Parameters { get; }

            public Task InvokeAsync(IBotInteractionContext context, IReadOnlyDictionary<string, object> arguments)
                => m_callback(context, arguments.GetRequired<string>(m_paramName));
        }

        private sealed class NoArgCommand : IRemoraSlashCommand
        {
            private readonly Func<IBotInteractionContext, Task> m_callback;

            public NoArgCommand(string name, string description, Func<IBotInteractionContext, Task> callback)
            {
                Name = name;
                Description = description;
                m_callback = callback;
            }

            public string Name { get; }
            public string Description { get; }
            public IReadOnlyList<RemoraSlashCommandParameter> Parameters { get; } = Array.Empty<RemoraSlashCommandParameter>();

            public Task InvokeAsync(IBotInteractionContext context, IReadOnlyDictionary<string, object> arguments) => m_callback(context);
        }
    }
}
