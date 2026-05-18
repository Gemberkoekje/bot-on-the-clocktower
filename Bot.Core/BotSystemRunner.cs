using Bot.Api;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotSystemRunner
    {
        private readonly IBotClient m_client;
        private readonly IFinalShutdownService m_finalShutdown;

        public BotSystemRunner(IBotClient client, IFinalShutdownService finalShutdown, IVersionProvider versionProvider)
        {
            m_client = client;
            m_finalShutdown = finalShutdown;
            versionProvider.InitializeVersions();
        }

        public async Task RunAsync()
        {
            await m_client.ConnectAsync();
            await m_finalShutdown.ReadyToShutdown;
            await m_client.DisconnectAsync();
        }
    }
}
