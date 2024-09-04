using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator.GameProviders;

public interface IGameProviderRepository
{
	ValueTask AddFavoriteGameAsync(Guid brandId, FavoriteGameInfo gameInfo);

	IAsyncEnumerable<GameProviderStatus> ListNotActiveSubGameProvidersAsync(Guid brandId, CancellationToken cancellationToken = default);

	IAsyncEnumerable<BrandGameInfo> QueryAllGamesAsync(Guid brandId);

	IAsyncEnumerable<GameInfo> QueryGameByGameTypeAsync(Guid brandId, GamePlatformType[] gamePlatformTypes, GameType type);

	Task<int> TotalGameByGameTypeAsync(Guid brandId, GamePlatformType[] gamePlatformTypes, GameType type);

	IAsyncEnumerable<BrandSubGameProviderInfo> QueryAllSubGameProvidersAsync(Guid brandId);

	IAsyncEnumerable<FavoriteGameInfo> QueryPlayerFavoriteGamesAsync(Guid brandId, string playerCode);

	ValueTask RemoveFavoriteGameAsync(string playerCode, int gameId);
}