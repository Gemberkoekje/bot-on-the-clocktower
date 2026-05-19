using System.ComponentModel;
using System.Threading.Tasks;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Bot.Remora
{
    /// <summary>
    /// Minimal Remora-native slash command used to validate that the gateway,
    /// command registration, and interaction dispatch are all working end-to-end.
    /// Once /ping responds with "pong" in Discord, broader commands can be re-enabled.
    /// </summary>
    public sealed class PingCommand : CommandGroup
    {
        private readonly FeedbackService m_feedback;

        public PingCommand(FeedbackService feedback)
        {
            m_feedback = feedback;
        }

        [Command("ping")]
        [Description("Replies with pong.")]
        public async Task<IResult> HandlePingAsync()
        {
            return (Result)await m_feedback.SendContextualAsync("pong");
        }
    }
}
