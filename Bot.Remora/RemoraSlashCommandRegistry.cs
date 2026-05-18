using System;
using System.Collections.Generic;
using System.Linq;

namespace Bot.Remora
{
    public class RemoraSlashCommandRegistry
    {
        private readonly List<Type> m_sourceTypes = new();
        private readonly List<Func<IServiceProvider, IRemoraSlashCommandSource>> m_factories = new();

        public IReadOnlyList<Type> SourceTypes => m_sourceTypes;

        public void AddSource<T>(Func<IServiceProvider, T> factory) where T : IRemoraSlashCommandSource
        {
            m_sourceTypes.Add(typeof(T));
            m_factories.Add(sp => factory(sp));
        }

        public IEnumerable<IRemoraSlashCommand> ResolveCommands(IServiceProvider serviceProvider)
        {
            foreach (var factory in m_factories)
            {
                IRemoraSlashCommandSource source = factory(serviceProvider);
                foreach (var command in source.GetCommands())
                {
                    yield return command;
                }
            }
        }
    }
}
