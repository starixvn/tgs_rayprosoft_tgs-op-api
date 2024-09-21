using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Extensions;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.GameTypes;

namespace TGS.Operator.Repositories.Services;


public static class GameService
{

	public static IEnumerable<BrandGame> OrderBy(IEnumerable<BrandGame> query, OrderType? orderType, int pageIndex, int pageSize)
	{
		var orderedList = orderType switch
		{
			OrderType.Hot => query.OrderByDescending(g => g.Tag & GameTag.Hot).ThenBy(g => g.DisplayOrder),
			OrderType.Ascending => query.OrderBy(g => g.Game.Name),
			OrderType.Descending => query.OrderByDescending(g => g.Game.Name),
			OrderType.New => query.OrderByDescending(g => g.Tag & GameTag.New).ThenBy(g => g.DisplayOrder),
			_ => query.OrderBy(g => g.DisplayOrder).ThenBy(g => g.Game.Name),
		};

		return orderedList.Skip((pageIndex - 1) * pageSize).Take(pageSize);
	}

	public static IEnumerable<GameInfo> MapCategory(IEnumerable<BrandGame> games, string cultureCode, IDictionary<Guid, Status> brandProviderStatus)
	{
		return games.Select(i =>
		{
			brandProviderStatus.TryGetValue(i.Game.GameProviderId, out Status status);
			var gameStatus = GetGameStatus(i.Status,
				i.Game.Status,
				i.Game.SubGameProvider.GameProvider.Status,
				status);
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
				GameStatus = gameStatus,
				DisplayOrder = i.DisplayOrder,
				Type = i.Game.Type,
				Tag = new TagInfo { New = i.Tag.IsNew(), Hot = i.Tag.IsHot(), Recommend = i.Tag.IsRecommend() },
			};
		}).OrderByDescending(g => g.GameStatus == GameStatus.Active);
	}


	private static GameStatus GetGameStatus(GameStatus brandGameStatus, GameStatus gameStatus, Status providerStatus, Status brandProviderStatus)
		=> brandGameStatus == GameStatus.Maintenance || gameStatus == GameStatus.Maintenance || providerStatus == Status.Maintenance || brandProviderStatus == Status.Maintenance
			? GameStatus.Maintenance
			: gameStatus == GameStatus.Testing
				? GameStatus.Testing
				: GameStatus.Active;

	private static IDictionary<string, JsonDocument> DeserializeLocalization(string localization)
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

}
