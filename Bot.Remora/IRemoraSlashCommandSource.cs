using System.Collections.Generic;

namespace Bot.Remora
{
    public interface IRemoraSlashCommandSource
    {
        IEnumerable<IRemoraSlashCommand> GetCommands();
    }
}
