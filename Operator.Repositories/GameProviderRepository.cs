using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Microsoft.EntityFrameworkCore;
using TGS.Core.DataServices.Tgs;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Extensions;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Game;
using TGS.Services.Operator.GameProviders;

namespace TGS.Operator.Repositories;

internal class GameProviderRepository(
	ITgsRepository tgsRepository) : IGameProviderRepository
{
	private readonly ITgsRepository _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));

	private readonly GameStatus[] _statuses = { GameStatus.Active, GameStatus.Maintenance, GameStatus.Testing };

	public async ValueTask AddFavoriteGameAsync(Guid brandId, FavoriteGameInfo gameInfo)
	{
		var fg = new PlayerFavoriteGame
		{
			PlayerCode = gameInfo.PlayerCode,
			BrandId = brandId,
			GameId = gameInfo.GameId,
			GameType = gameInfo.GameType,
			SubGameProviderCode = gameInfo.SubGpCode
		};

		_ = await _tgsRepository.PlayerFavoriteGames
			.AddAsync(fg)
			.ConfigureAwait(false);

		_ = await _tgsRepository.SaveChangesAsync().ConfigureAwait(false);
	}

	public IAsyncEnumerable<GameProviderStatus> ListNotActiveSubGameProvidersAsync(Guid brandId, CancellationToken cancellationToken = default)
	{
		var status = new[]
		{
			Status.Active,
			Status.Maintenance
		};

		var query = from brandGp in _tgsRepository.BrandGameProviders.AsQueryable()
					from gp in _tgsRepository.GameProviders
					from subGp in _tgsRepository.SubGameProviders
					where brandGp.GameProviderId == gp.Id
						&& brandGp.GameProviderId == subGp.GameProviderId
						&& brandGp.BrandId == brandId
						&& status.Contains(gp.Status)
						&& status.Contains(brandGp.Status)
					select new
					{
						SubGpCode = subGp.Code,
						GpStatus = gp.Status,
						BrandGpStatus = brandGp.Status
					};

		return query.ToAsyncEnumerable()
			.Where(info => info.BrandGpStatus == Status.Maintenance
				|| info.GpStatus == Status.Maintenance)
			.Select(info => new GameProviderStatus(
				info.SubGpCode,
				Status.Maintenance));
	}

	public IAsyncEnumerable<BrandGameInfo> QueryAllGamesAsync(Guid brandId)
	{
		var query = from bg in _tgsRepository.BrandGames.AsQueryable()
					from game in _tgsRepository.Games
					from sgp in _tgsRepository.SubGameProviders
					from gp in _tgsRepository.GameProviders
					where bg.BrandId == brandId
						&& bg.GameId == game.Id
						&& game.SubGameProviderCode == sgp.Code
						&& sgp.GameProviderId == gp.Id
						&& _statuses.Contains(bg.Status)
						&& _statuses.Contains(game.Status)
					select new
					{
						game.Id,
						game.Code,
						game.Name,
						game.Description,
						SubGpCode = game.SubGameProviderCode,
						GpId = gp.Id,
						GpCode = gp.Code,
						GameStatus = game.Status,
						BrandGameStatus = bg.Status,
						game.Type,
						game.PlatformType,
						game.AvailableToBrand,
						game.AvailableCurrencies,
						game.Localization,
						bg.Tag,
						bg.DisplayOrder
					};

		return query.ToAsyncEnumerable().Select(info =>
		{
			var gameStatus = GetGameStatus(info.BrandGameStatus, info.GameStatus);

			return new BrandGameInfo(
				info.Id,
				info.Code,
				info.Name,
				info.Description,
				info.SubGpCode,
				info.GpId,
				info.GpCode,
				gameStatus,
				info.Type,
				info.PlatformType,
				info.AvailableToBrand,
				info.AvailableCurrencies,
				info.Localization,
			info.Tag,
			info.DisplayOrder);
		});
	}

	public IAsyncEnumerable<BrandSubGameProviderInfo> QueryAllSubGameProvidersAsync(Guid brandId)
	{
		var status = new[]
		{
			Status.Active,
			Status.Maintenance
		};

		var query = from brandGp in _tgsRepository.BrandGameProviders.AsQueryable()
					from gp in _tgsRepository.GameProviders
					from subGp in _tgsRepository.SubGameProviders
					where brandGp.GameProviderId == gp.Id
						&& brandGp.GameProviderId == subGp.GameProviderId
						&& brandGp.BrandId == brandId
						&& status.Contains(gp.Status)
						&& status.Contains(brandGp.Status)
					select new
					{
						SubGpCode = subGp.Code,
						subGp.Name,
						subGp.Description,
						GpId = gp.Id,
						GpCode = gp.Code,
						gp.TransferWallet,
						GpStatus = gp.Status,
						BrandGpStatus = brandGp.Status,
						subGp.Localization
					};

		return query.ToAsyncEnumerable()
			.Select(info =>
			{
				var gpStatus = info.BrandGpStatus == Status.Maintenance || info.GpStatus == Status.Maintenance
					? Status.Maintenance
					: Status.Active;

				return new BrandSubGameProviderInfo(
					info.SubGpCode,
					info.Name,
					info.Description,
					info.GpId,
					info.GpCode,
					info.TransferWallet,
					gpStatus,
					info.Localization);
			});
	}

	public IAsyncEnumerable<BrandGameProviderInfo> QueryAllGameProvidersAsync(Guid brandId)
	{
		var status = new[]
		{
			Status.Active,
			Status.Maintenance
		};

		var query = from brandGp in _tgsRepository.BrandGameProviders.AsQueryable()
					from gp in _tgsRepository.GameProviders
					where brandGp.GameProviderId == gp.Id
						  && brandGp.BrandId == brandId
						  && status.Contains(gp.Status)
						  && status.Contains(brandGp.Status)
					select new
					{
						gp.Code,
						gp.Name,
						gp.Description,
						gp.TransferWallet,
						gp.Status,
						BrandGpStatus = brandGp.Status,
						gp.Localization
					};

		return query.ToAsyncEnumerable()
			.Select(info =>
			{
				var gpStatus = info.BrandGpStatus == Status.Maintenance || info.Status == Status.Maintenance
					? Status.Maintenance
					: Status.Active;

				return new BrandGameProviderInfo(
					info.Code,
					info.Name,
					info.Description,
					info.TransferWallet,
					gpStatus,
					info.Localization);
			});
	}
	public IAsyncEnumerable<FavoriteGameInfo> QueryPlayerFavoriteGamesAsync(Guid brandId, string playerCode)
	{
		var query = from fg in _tgsRepository.PlayerFavoriteGames.AsQueryable()
					where fg.BrandId == brandId && fg.PlayerCode == playerCode
					select fg;

		return query.ToAsyncEnumerable()
			.Select(fg => new FavoriteGameInfo(
				fg.PlayerCode,
				fg.GameId,
				fg.GameType,
				fg.SubGameProviderCode));
	}

	public async ValueTask RemoveFavoriteGameAsync(string playerCode, int gameId)
	{
		var fg = await _tgsRepository.PlayerFavoriteGames
			.FindAsync(playerCode, gameId)
			.ConfigureAwait(false);

		if (fg is not null)
			_ = _tgsRepository.PlayerFavoriteGames.Remove(fg);

		_ = await _tgsRepository.SaveChangesAsync().ConfigureAwait(false);
	}


	public async Task<IEnumerable<GameTagInfo>> QueryGameByTagsAsync(GetTopGamesByTagQuery query, GamePlatformType[] gamePlatformTypes)
	{
		var providersActivated = new HashSet<Guid>(
			await _tgsRepository.BrandGameProviders
			.Where(bp => bp.BrandId == query.BrandId && bp.Status == Status.Active)
			.Where(bp => query.ProviderCode.IsNullOrEmpty() || query.ProviderCode.Contains(bp.GameProvider.Code))
			.Select(bg => bg.GameProviderId)
			.ToListAsync()
		);

		var gamesQuery = _tgsRepository.BrandGames
			.Include(bg => bg.Game)
			.ThenInclude(g => g.SubGameProvider)
			.ThenInclude(sgp => sgp.GameProvider)
			.Where(bg => bg.BrandId == query.BrandId &&
						 bg.Game.AvailableToBrand &&
						 providersActivated.Contains(bg.Game.GameProviderId) &&
						 gamePlatformTypes.Contains(bg.Game.PlatformType) &&
						 _statuses.Contains(bg.Status) &&
						 _statuses.Contains(bg.Game.Status))
			.AsNoTracking();

		var groupedGames = new List<GameTagInfo>();

		foreach (var tag in query.Tags)
		{
			var tagQuery = gamesQuery
				.Where(g => (g.Tag & tag) != 0);

			var games = await tagQuery.OrderBy(g => g.DisplayOrder)
				.ThenBy(g => g.Game.Code)
				.Skip((query.PageIndex - 1) * query.PageSize).Take(query.PageSize)
				.ToListAsync();

			var gameCount = await tagQuery.CountAsync();

			groupedGames.Add(new GameTagInfo
			{
				Tag = tag,
				Total = gameCount,
				Games = games.Select(g => MapGameInfo(g, query.CultureCode))
				.OrderByDescending(g => g.IsActive)
				.ThenBy(g => g.DisplayOrder)
			});
		}

		return groupedGames;
	}

	private static GameInfo MapGameInfo(BrandGame info, string cultureCode)
	{
		var gameStatus = GetGameStatus(info.Status, info.Game.Status);
		var isActive = info.Game.SubGameProvider.GameProvider.Status == Status.Active && gameStatus == GameStatus.Active;
		var lang = DeserializeLocalization(info.Game.Localization).TryGetValue(cultureCode, out var value) ? value : JsonDocument.Parse("{}");

		return new GameInfo
		{
			Id = info.GameId,
			Code = info.Game.Code,
			Name = GetLocalization(lang.RootElement, "name", info.Game.Name),
			Description = GetLocalization(lang.RootElement, "description", info.Game.Description),
			GameProviderCode = info.Game.SubGameProvider.GameProvider.Code,
			GameProviderName = info.Game.SubGameProvider.GameProvider.Name,
			SubGameProviderCode = info.Game.SubGameProvider.Code,
			IsActive = isActive,
			DisplayOrder = info.DisplayOrder,
			Type = info.Game.Type,
			Tag = new TagInfo { New = info.Tag.IsNew(), Hot = info.Tag.IsHot(), Recommend = info.Tag.IsRecommend() }
		};
	}

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

	private static GameStatus GetGameStatus(GameStatus brandGameStatus, GameStatus gameStatus)
		=> brandGameStatus == GameStatus.Inactive || gameStatus == GameStatus.Inactive
			? GameStatus.Inactive
			: brandGameStatus == GameStatus.Testing || gameStatus == GameStatus.Testing
				? GameStatus.Testing
				: brandGameStatus == GameStatus.Maintenance || gameStatus == GameStatus.Maintenance
					? GameStatus.Maintenance
					: GameStatus.Active;
}