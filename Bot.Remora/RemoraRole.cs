using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Remora
{
    public class RemoraRole : IRole
    {
        public string Name { get; }

        public string Mention { get; }

        public ulong Id { get; }

        public bool IsThisBot { get; }

        public bool IsDeleted { get; private set; }

        public RemoraRole(ulong id, string name, bool isThisBot = false)
        {
            Id = id;
            Name = name;
            Mention = $"<@&{id}>";
            IsThisBot = isThisBot;
        }

        public Task DeleteAsync()
        {
            IsDeleted = true;
            return Task.CompletedTask;
        }
    }
}
