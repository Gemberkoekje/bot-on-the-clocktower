using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Remora
{
    public interface IRemoraCommandRegistrar
    {
        Task RegisterGlobalCommandsAsync(IReadOnlyCollection<IRemoraSlashCommand> commands);

        Task RegisterGuildCommandsAsync(IReadOnlyCollection<ulong> guildIds, IReadOnlyCollection<IRemoraSlashCommand> commands);

        Task ClearGuildCommandsAsync(IReadOnlyCollection<ulong> guildIds);
    }

    public sealed class NoOpRemoraCommandRegistrar : IRemoraCommandRegistrar
    {
        public Task RegisterGlobalCommandsAsync(IReadOnlyCollection<IRemoraSlashCommand> commands) => Task.CompletedTask;

        public Task RegisterGuildCommandsAsync(IReadOnlyCollection<ulong> guildIds, IReadOnlyCollection<IRemoraSlashCommand> commands) => Task.CompletedTask;

        public Task ClearGuildCommandsAsync(IReadOnlyCollection<ulong> guildIds) => Task.CompletedTask;
    }
}
