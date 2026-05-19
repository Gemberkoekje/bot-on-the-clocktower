using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace Bot.Remora
{
    internal sealed class RemoraGatewayInteractionResponder : IResponder<InteractionCreate>
    {
        private readonly IRemoraInteractionResponder m_interactionResponder;

        public RemoraGatewayInteractionResponder(IRemoraInteractionResponder interactionResponder)
        {
            m_interactionResponder = interactionResponder;
        }

        public async Task<Result> RespondAsync(InteractionCreate gatewayEvent, CancellationToken ct = default)
        {
            await m_interactionResponder.RespondAsync(gatewayEvent, ct);
            return Result.FromSuccess();
        }
    }
}
