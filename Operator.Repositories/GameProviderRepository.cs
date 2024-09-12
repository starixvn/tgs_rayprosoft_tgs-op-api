using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TGS.Core.DataServices.Tgs;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Extensions;
using TGS.Services.Operator;
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


	public async Task<IEnumerable<GameTagInfo>> QueryGameByTagsAsync(Guid brandId, GameTag[] tags, GamePlatformType[] gamePlatformTypes, int limit)
	{
		var providersActivated = new HashSet<Guid>(
			await _tgsRepository.BrandGameProviders
			.Where(bp => bp.BrandId == brandId && bp.Status == Status.Active)
			.Select(bg => bg.GameProviderId)
			.ToListAsync()
		);

		var gamesQuery = _tgsRepository.BrandGames
			.Include(bg => bg.Game)
				.ThenInclude(g => g.SubGameProvider)
					.ThenInclude(sgp => sgp.GameProvider)
			.Where(bg => bg.BrandId == brandId &&
						 bg.Game.AvailableToBrand &&
						 providersActivated.Contains(bg.Game.GameProviderId) &&
						 gamePlatformTypes.Contains(bg.Game.PlatformType) &&
						 _statuses.Contains(bg.Status) &&
						 _statuses.Contains(bg.Game.Status))
			.AsNoTracking()
			.Select(g => new
			{
				g.Game.Id,
				g.Game.Code,
				g.Game.Name,
				g.Game.Description,
				GameProviderCode = g.Game.SubGameProvider.GameProvider.Code,
				SubGameProvidercode = g.Game.SubGameProviderCode,
				GameProviderName = g.Game.SubGameProvider.GameProvider.Name,
				GameProviderStatus = g.Game.SubGameProvider.GameProvider.Status,
				g.Status,
				GameStatus = g.Game.Status,
				g.DisplayOrder,
				g.Game.Type,
				g.Game.Localization,
				g.Tag
			});

		var groupedGames = new List<GameTagInfo>();

		foreach (var tag in tags)
		{
			var games = await gamesQuery
				.Where(g => (g.Tag & tag) != 0)
				.OrderBy(g => g.DisplayOrder)
				.ThenBy(g => g.Code)
				.Take(limit)
				.ToListAsync();

			groupedGames.Add(new GameTagInfo
			{
				Tag = tag,
				Games = games.Select(g =>
				{
					var gameStatus = GetGameStatus(g.Status, g.GameStatus);
					var isActive = g.GameProviderStatus == Status.Active && gameStatus == GameStatus.Active;

					return new GameInfo
					{
						Id = g.Id,
						Code = g.Code,
						Name = g.Name,
						Description = g.Description,
						GameProviderCode = g.GameProviderCode,
						GameProviderName = g.GameProviderName,
						SubGameProviderCode = g.SubGameProvidercode,
						IsActive = isActive,
						DisplayOrder = g.DisplayOrder,
						Type = g.Type,
						Localization = string.IsNullOrEmpty(g.Localization)
							? new Dictionary<string, JsonDocument>()
							: JsonSerializer.Deserialize<Dictionary<string, JsonDocument>>(g.Localization),
						Tag = new TagInfo { New = g.Tag.IsNew(), Hot = g.Tag.IsHot(), Recommend = g.Tag.IsRecommend() }
					};
				})
				.OrderByDescending(g => g.IsActive)
				.ThenBy(g => g.DisplayOrder)
			});
		}

		return groupedGames;
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