using System;
using System.Collections.Generic;
using System.Threading;
using Tgs.ServiceUtility;
using TGS.Services.Operator.GameProviders;

namespace TGS.Services.Operator.ApplicationServices.GameProvider;

internal class GetNotActiveGameProvidersQueryService(IGameProviderRepository gameProviderRepository) : IQueryService<GetNotActiveGameProvidersQuery, GameProviderStatus>
{
	private readonly IGameProviderRepository _gameProviderRepository = gameProviderRepository ?? throw new ArgumentNullException(nameof(gameProviderRepository));

	public IAsyncEnumerable<GameProviderStatus> QueryAsync(
		GetNotActiveGameProvidersQuery query,
		CancellationToken cancellationToken = default)
		=> _gameProviderRepository.ListNotActiveSubGameProvidersAsync(query.BrandId, cancellationToken);
}