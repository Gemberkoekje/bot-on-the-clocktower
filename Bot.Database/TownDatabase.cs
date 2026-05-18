using Bot.Api;
using Bot.Api.Database;
using Marten;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Database
{
	public class TownDatabase : ITownDatabase
	{
		private readonly IDocumentStore m_documentStore;

		public TownDatabase(IDocumentStore documentStore)
		{
			m_documentStore = documentStore;
		}

		private static TownRecord RecordFromTownAndAuthorInfo(ITown town, ulong authorId, string? authorName)
		{
			return new TownRecord()
			{
				GuildId = town.Guild?.Id ?? 0,
				ControlChannel = town.ControlChannel?.Name,
				ControlChannelId = town.ControlChannel?.Id ?? 0,
				ChatChannel = town.ChatChannel?.Name,
				ChatChannelId = town.ChatChannel?.Id ?? 0,
				TownSquare = town.TownSquare?.Name,
				TownSquareId = town.TownSquare?.Id ?? 0,
				DayCategory = town.DayCategory?.Name,
				DayCategoryId = town.DayCategory?.Id ?? 0,
				NightCategory = town.NightCategory?.Name,
				NightCategoryId = town.NightCategory?.Id ?? 0,
				StorytellerRole = town.StorytellerRole?.Name,
				StorytellerRoleId = town.StorytellerRole?.Id ?? 0,
				VillagerRole = town.VillagerRole?.Name,
				VillagerRoleId = town.VillagerRole?.Id ?? 0,
				AuthorName = authorName,
				Author = authorId,
				Timestamp = DateTime.Now,
			};
		}

		private static TownRecord RecordFromTown(ITown town, IMember author) => RecordFromTownAndAuthorInfo(town, author.Id, author.DisplayName);

		public async Task<bool> AddTownAsync(ITown town, IMember author)
		{
			var newRec = RecordFromTown(town, author);
			await UpdateRecordAsync(newRec);
			return true;
		}

		public async Task<bool> UpdateTownAsync(ITown town)
		{
			if (town.Guild == null || town.ControlChannel == null)
				return false;

			if (await GetTownRecordAsync(town.Guild.Id, town.ControlChannel.Id) is not TownRecord oldRec)
				return false;

			var newRec = RecordFromTownAndAuthorInfo(town, oldRec.Author, oldRec.AuthorName);

			await UpdateRecordAsync(newRec);
			return true;
		}

		private async Task UpdateRecordAsync(TownRecord record)
		{
			using var session = m_documentStore.LightweightSession();

			var existing = await session.Query<TownRecord>()
				.FirstOrDefaultAsync(x => x.GuildId == record.GuildId && x.ControlChannelId == record.ControlChannelId);

			if (existing != null)
			{
				session.Delete(existing);
			}

			session.Store(record);
			await session.SaveChangesAsync();
		}

		public async Task<ITownRecord?> GetTownRecordAsync(ulong guildId, ulong channelId)
		{
			using var querySession = m_documentStore.QuerySession();
			return await querySession.Query<TownRecord>()
				.FirstOrDefaultAsync(x => x.GuildId == guildId && x.ControlChannelId == channelId);
		}

		public async Task<IEnumerable<ITownRecord>> GetTownRecordsAsync(ulong guildId)
		{
			using var querySession = m_documentStore.QuerySession();
			var records = await querySession.Query<TownRecord>()
				.Where(x => x.GuildId == guildId)
				.ToListAsync();
			return records;
		}

		public async Task<IEnumerable<TownKey>> GetAllTowns()
		{
			using var querySession = m_documentStore.QuerySession();
			var documents = await querySession.Query<TownRecord>().ToListAsync();
			return documents.Select(x => new TownKey(x.GuildId, x.ControlChannelId));
		}

		public async Task<bool> DeleteTownAsync(TownKey townKey)
		{
			using var session = m_documentStore.LightweightSession();
			var existing = await session.Query<TownRecord>()
				.FirstOrDefaultAsync(x => x.GuildId == townKey.GuildId && x.ControlChannelId == townKey.ControlChannelId);

			if (existing == null)
			{
				return false;
			}

			session.Delete(existing);
			await session.SaveChangesAsync();
			return true;
		}

		public async Task<ITownRecord?> GetTownRecordByNameAsync(ulong guildId, string townName)
		{
			using var querySession = m_documentStore.QuerySession();
			return await querySession.Query<TownRecord>()
				.FirstOrDefaultAsync(x => x.GuildId == guildId && x.DayCategory == townName);
		}

		public class MissingGuildInfoDatabaseException : Exception { }
	}
}
