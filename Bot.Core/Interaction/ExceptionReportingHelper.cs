using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core.Interaction
{
    public static class ExceptionReportingHelper
    {
        private const int MaximumMessageLength = 2000; // Hardcoded in DSharpPlus
        public static async Task TrySendExceptionToMemberAsync(string identifier, IMember member, Exception e)
        {
            try
            {
                string messageMinusStackTrace = $"Bot on the Clocktower encountered an error.\n\nPlease consider reporting the error at <https://github.com/Gemberkoekje/bot-on-the-clocktower/issues> and including all the information below:\n\n{identifier}\nException: `{e.GetType().Name}`\nMessage:   `{e.Message}`\nStack trace:\n";
                int availableForStackTrace = MaximumMessageLength - messageMinusStackTrace.Length - 6;
                string fullMessage = $"{messageMinusStackTrace}{(e.StackTrace == null ? "n/a" : availableForStackTrace > 0 ? $"```{GetRangeOrFull(e.StackTrace!, availableForStackTrace)}```" : "(2long)")}";
                await member.SendMessageAsync(GetRangeOrFull(fullMessage, MaximumMessageLength));
            }
            catch (Exception dmException)
            {
                Console.Error.WriteLine($"ExceptionReportingHelper: failed to send DM to member {member.Id}. Original={e.GetType().Name}: {e.Message}; DmError={dmException.GetType().Name}: {dmException.Message}");
                Console.Error.WriteLine($"ExceptionReportingHelper: original stack trace: {e}");
            }

            static string GetRangeOrFull(string str, int max)
            {
                return max < str.Length ? str[..max] : str;
            }
        }
    }
}
