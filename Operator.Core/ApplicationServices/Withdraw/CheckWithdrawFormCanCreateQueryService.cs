using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

public class CheckWithdrawFormCanCreateQueryService(IPaymentServiceClient paymentServiceClient) : IGetService<CreateWithdrawFormCommand, bool>
{
	private readonly IPaymentServiceClient _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));

	public async ValueTask<bool> GetAsync(CreateWithdrawFormCommand query, CancellationToken cancellationToken = default)
	{
		if (query.Amount <= 0)
			return false;

		var playerBankInfo = await _paymentServiceClient
			.GetPlayerBankInfosAsync(query.PlayerCode, cancellationToken)
			.FirstAsync(b => b.Id == query.BankInfoId, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return await _paymentServiceClient
			.GetBrandPaymentsAsync(query.BrandId, cancellationToken)
			.AnyAsync(bp => bp.ChannelId == playerBankInfo.PaymentChannelId, cancellationToken)
			.ConfigureAwait(false);
	}
}