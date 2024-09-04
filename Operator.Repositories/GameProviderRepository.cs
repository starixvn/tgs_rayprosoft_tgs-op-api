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

	public IAsyncEnumerable<GameInfo> QueryGameByGameTypeAsync(Guid brandId, GamePlatformType[] gamePlatformTypes, GameType type)
	{
		var query = _tgsRepository.BrandGames
			.Where(bg => bg.BrandId == brandId && bg.Game.AvailableToBrand && gamePlatformTypes.Contains(bg.Game.PlatformType)
											   && _statuses.Contains(bg.Status)
											   && _statuses.Contains(bg.Game.Status)
											   && (type == bg.Game.Type || type == 0))
			.Select(bg => new
			{
				bg.Game.Id,
				bg.Game.Code,
				bg.Game.Name,
				bg.Game.Description,
				GameStatus = bg.Game.Status,
				SubGameProviderStatus = bg.Game.GameProvider.Status,
				gameProviderCode = bg.Game.SubGameProvider.Code,
				BrandGameStatus = bg.Status,
				bg.Game.Type,
				bg.Game.Localization,
				bg.Tag,
				bg.DisplayOrder,
				IsTransferWallet = bg.Game.GameProvider.TransferWallet
			})
			.AsNoTracking();

		return query.ToAsyncEnumerable().Select(info =>
		{
			var gameStatus = GetGameStatus(info.BrandGameStatus, info.GameStatus);
			var isActive = info.SubGameProviderStatus == Status.Active && gameStatus == GameStatus.Active;
			return new GameInfo
			{
				Id = info.Id,
				Code = info.Code,
				Name = info.Name,
				Description = info.Description,
				GameProviderCode = info.gameProviderCode,
				IsActive = isActive,
				DisplayOrder = info.DisplayOrder,
				Type = info.Type,
				Tag = new TagInfo
				{
					New = info.Tag.IsNew(),
					Hot = info.Tag.IsHot(),
					Recommend = info.Tag.IsRecommend()
				},
				Localization = string.IsNullOrEmpty(info.Localization)
						? []
					: JsonSerializer.Deserialize<Dictionary<string,
				JsonDocument>>(
					info.Localization)
			};
		});
	}

	public async Task<int> TotalGameByGameTypeAsync(Guid brandId, GamePlatformType[] gamePlatformTypes, GameType type)
	{
		return await _tgsRepository.BrandGames
			.AsNoTracking()
			.CountAsync(bg => bg.BrandId == brandId && gamePlatformTypes.Contains(bg.Game.PlatformType)
											   && _statuses.Contains(bg.Status)
											   && _statuses.Contains(bg.Game.Status)
											   && (type == bg.Game.Type || type == 0));
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

	private static GameStatus GetGameStatus(GameStatus brandGameStatus, GameStatus gameStatus)
		=> brandGameStatus == GameStatus.Inactive || gameStatus == GameStatus.Inactive
			? GameStatus.Inactive
			: brandGameStatus == GameStatus.Testing || gameStatus == GameStatus.Testing
				? GameStatus.Testing
				: brandGameStatus == GameStatus.Maintenance || gameStatus == GameStatus.Maintenance
					? GameStatus.Maintenance
					: GameStatus.Active;
}