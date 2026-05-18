using Bot.Api.Database;
using System;

namespace Bot.Database
{
    public class TownRecord : ITownRecord
    {
        public string Id { get; set; } = string.Empty;

        public ulong GuildId { get; set; }
        public string? ControlChannel { get; set; }
        public ulong ControlChannelId { get; set; }
        public string? ChatChannel { get; set; }
        public ulong? ChatChannelId { get; set; }
        public string? TownSquare { get; set; }
        public ulong TownSquareId { get; set; }
        public string? DayCategory { get; set; }
        public ulong DayCategoryId { get; set; }
        public string? NightCategory { get; set; }
        public ulong NightCategoryId { get; set; }
        public string? StorytellerRole { get; set; }
        public ulong StorytellerRoleId { get; set; }
        public string? VillagerRole { get; set; }
        public ulong VillagerRoleId { get; set; }
        public string? AuthorName { get; set; }
        public ulong Author { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
