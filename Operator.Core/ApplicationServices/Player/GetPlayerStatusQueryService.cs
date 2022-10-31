using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class GetPlayerStatusQueryService : IGetService<GetPlayerStatusQuery, PlayerStatus>
{
	private readonly IPlayerServiceClient _playerServiceClient;

	public GetPlayerStatusQueryService(IPlayerServiceClient playerServiceClient)
	{
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
	}

	public ValueTask<PlayerStatus> GetAsync(
		GetPlayerStatusQuery query,
		CancellationToken cancellationToken = default)
		=> _playerServiceClient.GetPlayerStatusAsync(query.PlayerCode, cancellationToken);
}
