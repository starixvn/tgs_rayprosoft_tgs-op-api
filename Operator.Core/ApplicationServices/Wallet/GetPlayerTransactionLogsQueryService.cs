using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.CashService.Client;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Wallet;

internal class GetPlayerTransactionLogsQueryService(
	IPlayerServiceClient playerServiceClient,
	ICashServiceClient cashServiceClient) : IGetService<GetPlayerTransactionLogsQuery, PagingResponse<TransactionLog>>
{
	private readonly ICashServiceClient _cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
	private readonly IPlayerServiceClient _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));

	public async ValueTask<PagingResponse<TransactionLog>> GetAsync(
		GetPlayerTransactionLogsQuery query,
		CancellationToken cancellationToken = default)
	{
		var playerProps = await _playerServiceClient.GetPlayerPropertiesAsync(
			query.PlayerCode,
			cancellationToken).ConfigureAwait(false);

		var walletId = Guid.Parse(playerProps["DefaultWalletId"]);

		var paging = new PagingConditions(query.Page, query.Rows);

		return await _cashServiceClient.QueryTransactionLogsAsync(
			walletId,
			query.From,
			query.To,
			[6, 4100, 3800, 3850, 3860, 4, 6500],
			paging,
			cancellationToken).ConfigureAwait(false);
	}
}