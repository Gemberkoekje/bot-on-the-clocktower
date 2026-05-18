using System;
using System.Collections.Generic;
using System.Linq;

namespace Bot.Remora
{
    public class RemoraSlashCommandRegistry
    {
        private readonly List<Type> m_sourceTypes = new();
        private readonly List<Func<IRemoraSlashCommandSource>> m_sourceFactories = new();

        public IReadOnlyList<Type> SourceTypes => m_sourceTypes;

        public void AddSource<T>(T source) where T : IRemoraSlashCommandSource
        {
            m_sourceTypes.Add(typeof(T));
            m_sourceFactories.Add(() => source);
        }

        public void AddSource<T>(Func<T> sourceFactory) where T : IRemoraSlashCommandSource
        {
            m_sourceTypes.Add(typeof(T));
            m_sourceFactories.Add(() => sourceFactory());
        }

        public IEnumerable<IRemoraSlashCommand> ResolveCommands()
        {
            foreach (var sourceFactory in m_sourceFactories)
            {
                IRemoraSlashCommandSource source = sourceFactory();
                foreach (var command in source.GetCommands())
                {
                    yield return command;
                }
            }
        }
    }
}
