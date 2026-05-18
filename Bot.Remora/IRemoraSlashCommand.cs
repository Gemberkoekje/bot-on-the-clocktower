using System.Collections.Generic;
using System.Threading.Tasks;
using Bot.Api;

namespace Bot.Remora
{
    public enum RemoraSlashCommandParameterType
    {
        None = 0,
        String,
        Boolean,
        User,
        Role,
        Channel,
    }

    public sealed class RemoraSlashCommandParameter
    {
        public string Name { get; }
        public string Description { get; }
        public RemoraSlashCommandParameterType ParameterType { get; }
        public bool IsRequired { get; }

        public RemoraSlashCommandParameter(string name, string description, RemoraSlashCommandParameterType parameterType, bool isRequired)
        {
            Name = name;
            Description = description;
            ParameterType = parameterType;
            IsRequired = isRequired;
        }
    }

    public interface IRemoraSlashCommand
    {
        string Name { get; }
        string Description { get; }
        IReadOnlyList<RemoraSlashCommandParameter> Parameters { get; }
        Task InvokeAsync(IBotInteractionContext context, IReadOnlyDictionary<string, object> arguments);
    }
}
