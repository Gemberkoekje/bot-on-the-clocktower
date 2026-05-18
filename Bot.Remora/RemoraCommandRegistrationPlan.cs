using System.Collections.Generic;

namespace Bot.Remora
{
    public sealed class RemoraCommandRegistrationPlan
    {
        public string DeployType { get; }

        public bool RegisterGlobalCommands { get; }

        public bool ClearDevGuildCommands { get; }

        public IReadOnlyCollection<ulong> DevGuildIds { get; }

        public RemoraCommandRegistrationPlan(string deployType, bool registerGlobalCommands, bool clearDevGuildCommands, IReadOnlyCollection<ulong> devGuildIds)
        {
            DeployType = deployType;
            RegisterGlobalCommands = registerGlobalCommands;
            ClearDevGuildCommands = clearDevGuildCommands;
            DevGuildIds = devGuildIds;
        }
    }
}
