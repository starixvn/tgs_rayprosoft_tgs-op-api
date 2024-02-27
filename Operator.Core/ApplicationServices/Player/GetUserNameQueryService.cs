using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class GetUserNameQueryService(IPlayerServiceClient playerServiceClient) : IGetService<GetUserNameQuery, bool>
{
	private readonly IPlayerServiceClient _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));

	public async ValueTask<bool> GetAsync(GetUserNameQuery query, CancellationToken cancellationToken = default)
	{
		var playerInfo = await _playerServiceClient
			.GetPlayerInfoByUserNameAsync(query.BrandId, query.Username, cancellationToken)
			.ConfigureAwait(false);

		return playerInfo is not null;
	}
}