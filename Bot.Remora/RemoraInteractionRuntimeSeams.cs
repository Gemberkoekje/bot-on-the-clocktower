using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Objects;

namespace Bot.Remora
{
    internal interface IRemoraInteractionResponder
    {
        Task RespondAsync(IInteraction interaction, CancellationToken cancellationToken = default);
    }

    internal interface IRemoraSlashCommandDispatcher
    {
        Task DispatchAsync(IInteraction interaction, CancellationToken cancellationToken = default);
    }

    internal interface IRemoraComponentDispatcher
    {
        Task<bool> DispatchAsync(IInteraction interaction, CancellationToken cancellationToken = default);
    }

    internal sealed class NoOpRemoraInteractionResponder : IRemoraInteractionResponder
    {
        public Task RespondAsync(IInteraction interaction, CancellationToken cancellationToken = default)
        {
            Debug.WriteLine("Bot.Remora: interaction responder is not yet wired.");
            return Task.CompletedTask;
        }
    }

    internal sealed class NoOpRemoraSlashCommandDispatcher : IRemoraSlashCommandDispatcher
    {
        public Task DispatchAsync(IInteraction interaction, CancellationToken cancellationToken = default)
        {
            Debug.WriteLine("Bot.Remora: slash command dispatcher is not yet wired.");
            return Task.CompletedTask;
        }
    }

    internal sealed class NoOpRemoraComponentDispatcher : IRemoraComponentDispatcher
    {
        public Task<bool> DispatchAsync(IInteraction interaction, CancellationToken cancellationToken = default)
        {
            Debug.WriteLine("Bot.Remora: component dispatcher is not yet wired.");
            return Task.FromResult(false);
        }
    }
}
