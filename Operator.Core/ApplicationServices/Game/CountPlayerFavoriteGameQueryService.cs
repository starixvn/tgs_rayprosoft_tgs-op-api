using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using TGS.Services.Operator.GameProviders;

namespace TGS.Services.Operator.ApplicationServices.Game;

internal class CountPlayerFavoriteGameQueryService(IGameProviderRepository gameProviderRepository) : IGetService<CountPlayerFavoriteGameQuery, int>
{
	private readonly IGameProviderRepository _gameProviderRepository = gameProviderRepository ?? throw new ArgumentNullException(nameof(gameProviderRepository));

	public ValueTask<int> GetAsync(CountPlayerFavoriteGameQuery query, CancellationToken cancellationToken = default)
		=> _gameProviderRepository
			.QueryPlayerFavoriteGamesAsync(query.BrandId, query.PlayerCode)
			.CountAsync(cancellationToken);
}