using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TGS.Core.DataServices.Tgs;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Extensions;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Category;
using TGS.Services.Operator.GameProviders;

namespace TGS.Operator.Repositories
{
	public class CategoryRepository(ITgsRepository tgsRepository) : ICategoryRepository
	{
		private readonly ITgsRepository _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));

		private readonly GameStatus[] _statuses = { GameStatus.Active, GameStatus.Maintenance, GameStatus.Testing };
		public async Task<IEnumerable<Category>> QueryGameByGameTypesAsync(GetCategoriesQuery query, GamePlatformType[] platformTypes)
		{
			var groupedGames = _tgsRepository.BrandGames
				.Include(bg => bg.Game)
				.ThenInclude(g => g.SubGameProvider)
				.ThenInclude(sgp => sgp.GameProvider)
				.Where(bg => bg.BrandId == query.BrandId &&
							 bg.Game.AvailableToBrand &&
							 platformTypes.Contains(bg.Game.PlatformType) &&
							 _statuses.Contains(bg.Status) &&
							 _statuses.Contains(bg.Game.Status) &&
							 (query.Types.IsEmpty() || query.Types.Contains(bg.Game.Type)))
				.AsNoTracking()
				.GroupBy(bg => bg.Game.Type);

			var paginatedResults = await groupedGames
				.Select(g => new
				{
					GameType = g.Key,
					total = g.Count(),
					Games = g.OrderBy(g => g.DisplayOrder).ThenBy(g => g.Game.Code).Skip((query.PageIndex - 1) * query.PageSize).Take(query.PageSize).ToList()
				})
				.ToListAsync();


			return paginatedResults.Select(info => MapCategory(info.GameType, info.total, info.Games, query.CultureCode));

		}

		public async Task<IEnumerable<Category>> QueryRecommendedGamesByTypesAsync(GetRecommendedGamesByTypesQuery query, GamePlatformType[] gamePlatformTypes)
		{
			var groupedGames = _tgsRepository.BrandGames
				.Include(bg => bg.Game)
				.ThenInclude(g => g.SubGameProvider)
				.ThenInclude(sgp => sgp.GameProvider)
				.Where(bg => bg.BrandId == query.BrandId &&
							 bg.Game.AvailableToBrand &&
							 gamePlatformTypes.Contains(bg.Game.PlatformType) &&
							 _statuses.Contains(bg.Status) &&
							 _statuses.Contains(bg.Game.Status) &&
							 ((bg.Tag & GameTag.Recommend) != 0) &&
							 (query.Types.IsEmpty() || query.Types.Contains(bg.Game.Type)))
				.AsNoTracking()
				.GroupBy(bg => bg.Game.Type);

			var paginatedResults = await groupedGames
				.Select(g => new
				{
					GameType = g.Key,
					total = g.Count(),
					Games = g.OrderBy(g => g.DisplayOrder).ThenBy(g => g.Game.Code).Take(query.Limit).ToList()
				})
				.ToListAsync();


			return paginatedResults.Select(info => MapCategory(info.GameType, info.total, info.Games, query.CultureCode));

		}

		private Category MapCategory(GameType type, int total, IEnumerable<BrandGame> games, string cultureCode)
		{
			return new Category
			{
				Total = total,
				CategoryName = type.ToString(),
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
						GameProviderCode = i.Game.SubGameProvider.Code,
						IsActive = isActive,
						DisplayOrder = i.DisplayOrder,
						Type = type,
						Tag = new TagInfo { New = i.Tag.IsNew(), Hot = i.Tag.IsHot(), Recommend = i.Tag.IsRecommend() },
					};
				})
			};
		}

		private IDictionary<string, JsonDocument> DeserializeLocalization(string localization)
		{
			if (string.IsNullOrEmpty(localization))
			{
				return new Dictionary<string, JsonDocument>();
			}

			return JsonSerializer.Deserialize<Dictionary<string, JsonDocument>>(localization);
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
