using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Extensions;
using TGS.Operator.Repositories.Models;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.GameTypes;

namespace TGS.Operator.Repositories.Services;


public static class GameService
{

	public static IQueryable<GroupGameDto> OrderAndPagination(IQueryable<IGrouping<object, GameDto>> query,
		OrderType? orderType, int pageIndex, int pageSize)
	{
		return orderType switch
		{
			OrderType.Hot => query
				.Select(g => new GroupGameDto()
				{
					Key = g.Key,
					Total = g.Count(),
					Games = g.OrderByDescending(bg => bg.Tag & GameTag.Hot)
						.ThenBy(bg => bg.DisplayOrder)
						.ThenBy(bg => bg.Name)
						.Skip((pageIndex - 1) * pageSize)
						.Take(pageSize)
				}),
			OrderType.Ascending => query
				.Select(g => new GroupGameDto
				{
					Key = g.Key,
					Total = g.Count(),
					Games = g.OrderBy(bg => bg.Name)
						.Skip((pageIndex - 1) * pageSize)
						.Take(pageSize)
				}),
			OrderType.Descending => query
				.Select(g => new GroupGameDto
				{
					Key = g.Key,
					Total = g.Count(),
					Games = g.OrderByDescending(bg => bg.Name)
						.Skip((pageIndex - 1) * pageSize)
						.Take(pageSize)
				}),
			OrderType.New => query
				.Select(g => new GroupGameDto
				{
					Key = g.Key,
					Total = g.Count(),
					Games = g.OrderByDescending(bg => bg.Tag & GameTag.New)
						.ThenBy(bg => bg.DisplayOrder)
						.ThenBy(bg => bg.Name)
						.Skip((pageIndex - 1) * pageSize)
						.Take(pageSize)
				}),
			_ => query
				.Select(g => new GroupGameDto
				{
					Key = g.Key,
					Total = g.Count(),
					Games = g.OrderBy(bg => bg.DisplayOrder)
						.ThenBy(bg => bg.Name)
						.Skip((pageIndex - 1) * pageSize)
						.Take(pageSize)
				})
		};
	}
	public static IEnumerable<GameInfo> MapCategory(IEnumerable<GameDto> games, string cultureCode, IDictionary<Guid, Status> brandProviderStatus)
	{
		return games.Select(i =>
		{
			brandProviderStatus.TryGetValue(i.GameProviderId, out Status status);
			var gameStatus = GetGameStatus(i.BranGameStatus,
				i.Status,
				i.GameProviderStatus,
				status);
			var lang = DeserializeLocalization(i.Localization).TryGetValue(cultureCode, out var value)
				? value
				: JsonDocument.Parse("{}");
			return new GameInfo
			{
				Id = i.Id,
				Code = i.Code,
				Name = GetLocalization(
					lang.RootElement,
					"name",
					i.Name),
				Description = GetLocalization(
					lang.RootElement,
					"description",
					i.Description),
				GameProviderCode = i.GameProviderCode,
				GameProviderName = i.GameProviderName,
				GameStatus = gameStatus,
				DisplayOrder = i.DisplayOrder,
				Type = i.Type,
				Tag = new TagInfo { New = i.Tag.IsNew(), Hot = i.Tag.IsHot(), Recommend = i.Tag.IsRecommend() },
			};
		}).OrderByDescending(g => g.GameStatus == GameStatus.Active);
	}

	public static IEnumerable<GameDto> SelectGame(this IEnumerable<BrandGame> query)
	{
		return query.Select(bg => new GameDto
		{
			Id = bg.Game.Id,
			Code = bg.Game.Code,
			Name = bg.Game.Name,
			Description = bg.Game.Description,
			GameProviderCode = bg.Game.GameProvider.Code,
			GameProviderName = bg.Game.GameProvider.Name,
			GameProviderId = bg.Game.GameProvider.Id,
			GameProviderStatus = bg.Game.GameProvider.Status,
			BranGameStatus = bg.Status,
			Localization = bg.Game.Localization,
			DisplayOrder = bg.DisplayOrder,
			Type = bg.Game.Type,
			Tag = bg.Tag
		});
	}

	public static IQueryable<GameDto> SelectGameInfo(this IQueryable<BrandGame> query)
	{
		return query.Select(bg => new GameDto
		{
			Id = bg.Game.Id,
			Code = bg.Game.Code,
			Name = bg.Game.Name,
			Description = bg.Game.Description,
			GameProviderCode = bg.Game.SubGameProvider.GameProvider.Code,
			GameProviderName = bg.Game.SubGameProvider.GameProvider.Name,
			GameProviderId = bg.Game.SubGameProvider.GameProvider.Id,
			GameProviderStatus = bg.Game.SubGameProvider.GameProvider.Status,
			SubGameProviderCode = bg.Game.SubGameProvider.Code,
			BranGameStatus = bg.Status,
			Localization = bg.Game.Localization,
			DisplayOrder = bg.DisplayOrder,
			Type = bg.Game.Type,
			Tag = bg.Tag
		});
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
