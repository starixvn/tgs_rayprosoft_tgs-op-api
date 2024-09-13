using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using TGS.Core.Models.Extensions;
using TGS.Services.Operations;
using TGS.Services.Operator.GameProviders;

namespace TGS.Services.Operator.ApplicationServices.GameProvider;
public class GetGamesByProviderQueryService(IGameProviderRepository gameProviderRepository, IPlayerOperations playerOperations) : IGetService<GetGamesByProviderQuery, IEnumerable<ProviderGameListInfo>>
{
	private readonly IGameProviderRepository _gameProviderRepository =
	gameProviderRepository ?? throw new ArgumentNullException(nameof(gameProviderRepository));

	private readonly IPlayerOperations _playerOperations = playerOperations ?? throw new ArgumentNullException(nameof(playerOperations));

	public async ValueTask<IEnumerable<ProviderGameListInfo>> GetAsync(
		GetGamesByProviderQuery query,
		CancellationToken cancellationToken = default)
	{
		var gamePlatformTypes = query.PlatformType.ToGamePlatformTypes();

		var games = await _gameProviderRepository.QueryGamesByProviderAsync(query, gamePlatformTypes);

		return games;
	}
}
