using Bot.Api;
using Bot.Api.Database;
using System.Threading.Tasks;

namespace Bot.Core
{
    internal class GhostTownCleanup : IGhostTownCleanup
    {
        private readonly ITownMaintenance m_startupTownTasks;
        private readonly IBotClient m_botClient;
        private readonly ITownDatabase m_townDatabase;
        private readonly IGameMetricDatabase m_gameMetricDatabase;
        private readonly IDateTime m_dateTime;

        public GhostTownCleanup(
            ITownMaintenance startupTownTasks,
            IBotClient botClient,
            ITownDatabase townDatabase,
            IGameMetricDatabase gameMetricDatabase,
            IDateTime dateTime)
        {
            m_startupTownTasks = startupTownTasks;
            m_botClient = botClient;
            m_townDatabase = townDatabase;
            m_gameMetricDatabase = gameMetricDatabase;
            m_dateTime = dateTime;

            m_startupTownTasks.AddMaintenanceTask(CleanupGhostTown);
        }

        private async Task CleanupGhostTown(TownKey townKey)
        {
            Serilog.Log.Verbose("GhostTownCleanup: Checking town {townKey}", townKey);

            // If there was activity in the last 30 days, skip this town
            var mostRecent = await m_gameMetricDatabase.GetMostRecentGameAsync(townKey);
            if (mostRecent != null && mostRecent.Value.AddDays(30) > m_dateTime.Now)
                return;
            
            IGuild? guild = await m_botClient.GetGuildAsync(townKey.GuildId);
            IChannel? controlChan = null;

            if(guild != null)
            {
                controlChan = guild.GetChannel(townKey.ControlChannelId);
            }

            if(guild == null || controlChan == null)
            {
                Serilog.Log.Information("GhostTownCleanup: Deleting dead town {townKey}! Bad guild: {guildMissing}, Bad chan: {controlChanMissing}", townKey, guild==null, controlChan==null);
                // This town is a ghooooost
                await m_townDatabase.DeleteTownAsync(townKey);
            }

        }
    }
}
