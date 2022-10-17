using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TGS.Core.DataServices.Tgs;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Core.Models.Enums;
using TGS.Services.Operator;
using TGS.Services.Operator.GameProviders;

namespace TGS.Operator.Repositories;

internal class GameProviderRepository : IGameProviderRepository
{
    private readonly ITgsRepository _tgsRepository;

    public GameProviderRepository(
        ITgsRepository tgsRepository)
    {
        _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
    }

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

    public IAsyncEnumerable<BrandGameInfo> QueryAllGamesAsync(Guid brandId)
    {
        var status = new[]
        {
            GameStatus.Active,
            GameStatus.Maintenance,
            GameStatus.Testing
        };

        var query = from bg in _tgsRepository.BrandGames.AsQueryable()
                    from game in _tgsRepository.Games
                    from sgp in _tgsRepository.SubGameProviders
                    from gp in _tgsRepository.GameProviders
                    where bg.BrandId == brandId
                        && bg.GameId == game.Id
                        && game.SubGameProviderCode == sgp.Code
                        && sgp.GameProviderId == gp.Id
                        && status.Contains(bg.Status)
                        && status.Contains(game.Status)
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

    private GameStatus GetGameStatus(GameStatus brandGameStatus, GameStatus gameStatus)
    {
        if (brandGameStatus == GameStatus.Inactive || gameStatus == GameStatus.Inactive)
        {
            return GameStatus.Inactive;
        }

        if (brandGameStatus == GameStatus.Testing || gameStatus == GameStatus.Testing)
        {
            return GameStatus.Testing;
        }

        if (brandGameStatus == GameStatus.Maintenance || gameStatus == GameStatus.Maintenance)
        {
            return GameStatus.Maintenance;
        }

        return GameStatus.Active;
    }
}
