using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class GetPlayerMainDataQueryService : IGetService<GetPlayerMainDataQuery, Tgs.PlayerService.Client.PlayerInfo?>
{
	private readonly IPlayerServiceClient _playerServiceClient;

	public GetPlayerMainDataQueryService(
		IPlayerServiceClient playerServiceClient)
	{
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
	}

	public ValueTask<Tgs.PlayerService.Client.PlayerInfo?> GetAsync(
		GetPlayerMainDataQuery query,
		CancellationToken cancellationToken = default)
		=> _playerServiceClient.GetPlayerInfoAsync(query.PlayerCode, cancellationToken);
}
