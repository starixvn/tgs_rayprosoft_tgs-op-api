using System;
using System.Collections.Generic;
using System.Threading;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

internal class GetPlayerBankInfosQueryService : IQueryService<GetPlayerBankInfosQuery, PlayerBankInfo>
{
	private readonly IPaymentServiceClient _paymentServiceClient;

	public GetPlayerBankInfosQueryService(IPaymentServiceClient paymentServiceClient)
	{
		_paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
	}

	public IAsyncEnumerable<PlayerBankInfo> QueryAsync(GetPlayerBankInfosQuery query, CancellationToken cancellationToken = default)
		=> _paymentServiceClient.GetPlayerBankInfosAsync(query.PlayerCode, cancellationToken);
}
