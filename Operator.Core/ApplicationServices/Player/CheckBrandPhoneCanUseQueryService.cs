using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class CheckBrandPhoneCanUseQueryService(IPlayerServiceClient playerServiceClient) : IGetService<CheckBrandPhoneCanUseQuery, bool>
{
	private readonly IPlayerServiceClient _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));

	public async ValueTask<bool> GetAsync(CheckBrandPhoneCanUseQuery query, CancellationToken cancellationToken = default)
	{
		if (query.PlayerCode is null)
			return true;

		var player = await _playerServiceClient.GetPlayerInfoByPhoneAsync(
			query.BrandId,
			query.AreaCode,
			query.Phone,
			cancellationToken).ConfigureAwait(false);

		return player is null || query.PlayerCode is not null && player.PlayerCode == query.PlayerCode;
	}
}
