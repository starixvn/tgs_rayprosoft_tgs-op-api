using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class CheckPlayerBannedQueryService(
	IPlayerServiceClient playerServiceClient) : IGetService<CheckPlayerBannedQuery, bool>
{
	private readonly IPlayerServiceClient _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));

	public async ValueTask<bool> GetAsync(CheckPlayerBannedQuery query, CancellationToken cancellationToken = default)
	{
		var playerInfo = await _playerServiceClient
			.GetPlayerInfoByUserNameAsync(query.BrandId, query.UserName, cancellationToken)
			.ConfigureAwait(false);

		if (playerInfo is null)
			return false;

		var playerStatus = await _playerServiceClient
			.GetPlayerStatusAsync(playerInfo.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		return playerStatus == PlayerStatus.InBlacklist;
	}
}