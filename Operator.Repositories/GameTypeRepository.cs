using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Microsoft.EntityFrameworkCore;
using TGS.Core.DataServices.Tgs;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Extensions;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.GameTypes;
using TGS.Services.Operator.GameProviders;

namespace TGS.Operator.Repositories
{
	public class GameTypeRepository(ITgsRepository tgsRepository) : IGameTypeRepository
	{
		private readonly ITgsRepository _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));

		private readonly GameStatus[] _statuses = { GameStatus.Active, GameStatus.Maintenance, GameStatus.Testing };
		public async Task<IEnumerable<GameTypeModel>> QueryGameByGameTypesAsync(GetGameTypesQuery query, GamePlatformType[] platformTypes)
		{
			var providersActivated = _tgsRepository.BrandGameProviders
				.Where(bp => bp.BrandId == query.BrandId && bp.Status == Status.Active).Select(bg => bg.GameProviderId).ToList();

			var groupedGames = _tgsRepository.BrandGames
				.Include(bg => bg.Game)
					.ThenInclude(g => g.SubGameProvider)
					.ThenInclude(sgp => sgp.GameProvider)
				.Where(bg => bg.BrandId == query.BrandId &&
							 providersActivated.Contains(bg.Game.GameProviderId) &&
							 bg.Game.AvailableToBrand &&
							 platformTypes.Contains(bg.Game.PlatformType) &&
							 _statuses.Contains(bg.Status) &&
							 _statuses.Contains(bg.Game.Status) &&
							 (query.Types.IsNullOrEmpty() || query.Types.Contains(bg.Game.Type)) &&
							 (query.Tag == GameTag.None || (bg.Tag & query.Tag) != 0))
				.AsNoTracking()
				.GroupBy(bg => bg.Game.Type);

			var paginatedResults = await groupedGames
				.Select(g => new
				{
					GameType = g.Key,
					total = g.Count(),
					Games = g
						.OrderBy(bg => bg.DisplayOrder)
						.ThenBy(bg => bg.Game.Code)
						.Skip((query.PageIndex - 1) * query.PageSize)
						.Take(query.PageSize).ToList()
				})
				.ToListAsync();


			return paginatedResults.Select(info => MapCategory(info.GameType, info.total, info.Games, query.CultureCode));

		}
		private GameTypeModel MapCategory(GameType type, int total, IEnumerable<BrandGame> games, string cultureCode)
		{
			return new GameTypeModel
			{
				Total = total,
				CategoryName = type.ToString(),
				GameType = type,
				Games = games.Select(i =>
				{
					var gameStatus = GetGameStatus(i.Status, i.Game.Status);
					var isActive = i.Game.SubGameProvider.GameProvider.Status == Status.Active &&
								   gameStatus == GameStatus.Active;
					var lang = DeserializeLocalization(i.Game.Localization).TryGetValue(cultureCode, out var value)
						? value
						: JsonDocument.Parse("{}");
					return new GameInfo
					{
						Id = i.Game.Id,
						Code = i.Game.Code,
						Name = GetLocalization(
							lang.RootElement,
							"name",
							i.Game.Name),
						Description = GetLocalization(
							lang.RootElement,
							"description",
							i.Game.Description),
						GameProviderCode = i.Game.SubGameProvider.GameProvider.Code,
						GameProviderName = i.Game.SubGameProvider.GameProvider.Name,
						SubGameProviderCode = i.Game.SubGameProviderCode,
						IsActive = isActive,
						DisplayOrder = i.DisplayOrder,
						Type = type,
						Tag = new TagInfo { New = i.Tag.IsNew(), Hot = i.Tag.IsHot(), Recommend = i.Tag.IsRecommend() },
					};
				}).OrderByDescending(g => g.IsActive).ThenBy(g => g.DisplayOrder)
			};
		}

		private IDictionary<string, JsonDocument> DeserializeLocalization(string localization)
		{
			return (string.IsNullOrEmpty(localization) ? new Dictionary<string, JsonDocument>() : JsonSerializer.Deserialize<Dictionary<string, JsonDocument>>(localization))!;
		}

		private static string GetLocalization(JsonElement element, string propertyName, string defaultValue)
		{
			if (!element.TryGetProperty(propertyName, out var value))
			{
				return defaultValue;
			}

			var val = value.GetString();
			return string.IsNullOrEmpty(val) ? defaultValue : val;
		}

		private static GameStatus GetGameStatus(GameStatus brandGameStatus, GameStatus gameStatus)
			=> brandGameStatus == GameStatus.Inactive || gameStatus == GameStatus.Inactive
				? GameStatus.Inactive
				: brandGameStatus == GameStatus.Testing || gameStatus == GameStatus.Testing
					? GameStatus.Testing
					: brandGameStatus == GameStatus.Maintenance || gameStatus == GameStatus.Maintenance
						? GameStatus.Maintenance
						: GameStatus.Active;
	}
}
