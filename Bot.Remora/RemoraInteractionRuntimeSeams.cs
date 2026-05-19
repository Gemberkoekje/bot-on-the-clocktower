using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Bot.Remora
{
    internal interface IRemoraInteractionResponder
    {
        Task RespondAsync(CancellationToken cancellationToken = default);
    }

    internal interface IRemoraSlashCommandDispatcher
    {
        Task DispatchAsync(string commandName, IReadOnlyDictionary<string, object> arguments, CancellationToken cancellationToken = default);
    }

    internal interface IRemoraComponentDispatcher
    {
        Task<bool> DispatchAsync(string customId, IReadOnlyCollection<string> values, CancellationToken cancellationToken = default);
    }

    internal sealed class NoOpRemoraInteractionResponder : IRemoraInteractionResponder
    {
        public Task RespondAsync(CancellationToken cancellationToken = default)
        {
            Debug.WriteLine("Bot.Remora: interaction responder is not yet wired.");
            return Task.CompletedTask;
        }
    }

    internal sealed class NoOpRemoraSlashCommandDispatcher : IRemoraSlashCommandDispatcher
    {
        public Task DispatchAsync(string commandName, IReadOnlyDictionary<string, object> arguments, CancellationToken cancellationToken = default)
        {
            Debug.WriteLine("Bot.Remora: slash command dispatcher is not yet wired.");
            return Task.CompletedTask;
        }
    }

    internal sealed class NoOpRemoraComponentDispatcher : IRemoraComponentDispatcher
    {
        public Task<bool> DispatchAsync(string customId, IReadOnlyCollection<string> values, CancellationToken cancellationToken = default)
        {
            Debug.WriteLine("Bot.Remora: component dispatcher is not yet wired.");
            return Task.FromResult(false);
        }
    }
}
