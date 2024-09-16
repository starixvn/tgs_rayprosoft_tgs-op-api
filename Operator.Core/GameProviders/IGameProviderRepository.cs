using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TGS.Core.Models.Enums;
using TGS.Services.Operator.ApplicationServices.Game;

namespace TGS.Services.Operator.GameProviders;

public interface IGameProviderRepository
{
	ValueTask AddFavoriteGameAsync(Guid brandId, FavoriteGameInfo gameInfo);

	IAsyncEnumerable<GameProviderStatus> ListNotActiveSubGameProvidersAsync(Guid brandId, CancellationToken cancellationToken = default);

	IAsyncEnumerable<BrandGameInfo> QueryAllGamesAsync(Guid brandId);

	Task<IEnumerable<GameTagInfo>> QueryGameByTagsAsync(GetTopGamesByTagQuery query, GamePlatformType[] gamePlatformTypes);

	IAsyncEnumerable<BrandSubGameProviderInfo> QueryAllSubGameProvidersAsync(Guid brandId);

	IAsyncEnumerable<BrandGameProviderInfo> QueryAllGameProvidersAsync(Guid brandId);

	IAsyncEnumerable<FavoriteGameInfo> QueryPlayerFavoriteGamesAsync(Guid brandId, string playerCode);

	ValueTask RemoveFavoriteGameAsync(string playerCode, int gameId);
}