using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class CheckBrandEmailQueryService(IPlayerServiceClient playerServiceClient) : IGetService<CheckBrandEmailQuery, bool>
{
	private readonly IPlayerServiceClient _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));

	public async ValueTask<bool> GetAsync(CheckBrandEmailQuery query, CancellationToken cancellationToken = default)
	{
		var player = await _playerServiceClient
			.GetPlayerInfoByEmailAsync(query.BrandId, query.Email, cancellationToken)
			.ConfigureAwait(false);

		return player is not null && player.PlayerCode != query.PlayerCode;
	}
}