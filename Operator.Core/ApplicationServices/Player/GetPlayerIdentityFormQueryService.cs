using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class GetPlayerIdentityFormQueryService(IPlayerRepository playerRepository) : IGetService<GetPlayerIdentityFormQuery, PlayerIdentityFormInfo>
{
	private readonly IPlayerRepository _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));

	public async ValueTask<PlayerIdentityFormInfo> GetAsync(GetPlayerIdentityFormQuery query, CancellationToken cancellationToken = default)
		=> await _playerRepository
			.GetPlayerIdentityFormQueryAsync(query.PlayerCode)
			.ConfigureAwait(false);
}
