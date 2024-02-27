using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Game;

internal class GetGamesQueryService(
	IQueryService<GetAllGamesByGameTypeQuery, GameInfo> queryService) : IQueryService<GetGamesQuery, GameInfo>
{
	private readonly IQueryService<GetAllGamesByGameTypeQuery, GameInfo> _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));

	public IAsyncEnumerable<GameInfo> QueryAsync(
		GetGamesQuery query,
		CancellationToken cancellationToken = default)
	{
		var allQuery = new GetAllGamesByGameTypeQuery(
			query.BrandId,
			query.PlayerType,
			query.PlatformType,
			query.Types);

		var allGames = _queryService.QueryAsync(allQuery, cancellationToken);

		return allGames.Where(
			gameInfo => gameInfo.GameProviderCode == query.SubGameProviderCode);
	}
}