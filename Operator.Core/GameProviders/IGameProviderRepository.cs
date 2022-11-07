using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TGS.Services.Operator.GameProviders;

public interface IGameProviderRepository
{
	ValueTask AddFavoriteGameAsync(Guid brandId, FavoriteGameInfo gameInfo);

	IAsyncEnumerable<BrandGameInfo> QueryAllGamesAsync(Guid brandId);

	IAsyncEnumerable<BrandSubGameProviderInfo> QueryAllSubGameProvidersAsync(Guid brandId);

	IAsyncEnumerable<FavoriteGameInfo> QueryPlayerFavoriteGamesAsync(Guid brandId, string playerCode);

	ValueTask RemoveFavoriteGameAsync(string playerCode, int gameId);
}
