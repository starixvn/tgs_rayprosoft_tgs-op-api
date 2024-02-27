using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.CashService.Client;
using Tgs.ServiceUtility;
using TGS.Services.Operations;

namespace TGS.Services.Operator.ApplicationServices.Wallet;

internal class GetBalanceQueryService(ICashServiceClient cashServiceClient,
	IPlayerOperations playerOperations,
	IPlayerOnlineRepository playerOnlineRepository) : IGetService<GetBalanceQuery, BalanceInfo>
{
	private readonly ICashServiceClient _cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
	private readonly IPlayerOnlineRepository _playerOnlineRepository = playerOnlineRepository ?? throw new ArgumentNullException(nameof(playerOnlineRepository));
	private readonly IPlayerOperations _playerOperations = playerOperations ?? throw new ArgumentNullException(nameof(playerOperations));

	public async ValueTask<BalanceInfo> GetAsync(GetBalanceQuery query, CancellationToken cancellationToken = default)
	{
		var playerData = await _playerOperations
			.GetCachedPlayerAsync(query.PlayerCode)
			.ConfigureAwait(false);
		var balance = await _cashServiceClient
			.GetBalanceAsync(playerData.DefaultWalletId, cancellationToken)
			.ConfigureAwait(false);
		await _playerOnlineRepository
			 .SetAsync(playerData.BrandId, playerData.PlayerCode)
			 .ConfigureAwait(false);

		return new BalanceInfo(balance);
	}
}