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

	IAsyncEnumerable<GameTagInfo> QueryGameByTagsAsync(Guid brandId, GameTag[] tags, GamePlatformType[] gamePlatformTypes, int limit);

	IAsyncEnumerable<BrandSubGameProviderInfo> QueryAllSubGameProvidersAsync(Guid brandId);

	IAsyncEnumerable<FavoriteGameInfo> QueryPlayerFavoriteGamesAsync(Guid brandId, string playerCode);

	ValueTask RemoveFavoriteGameAsync(string playerCode, int gameId);
}