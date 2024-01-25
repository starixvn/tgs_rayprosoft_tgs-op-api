using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Deposit;

internal class GetBankDepositFromQueryService(IPaymentServiceClient paymentServiceClient) : IGetService<GetBankDepositFormQuery, DepositFormInfo>
{
	private readonly IPaymentServiceClient _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));

	public async ValueTask<DepositFormInfo> GetAsync(GetBankDepositFormQuery query, CancellationToken cancellationToken = default)
	{
		var form = await _paymentServiceClient
			.GetDepositFormAsync(query.BrandId, query.FormId, cancellationToken)
			.ConfigureAwait(false);

		return form == null || form.PlayerCode != query.PlayerCode ? throw new UltraViresException() : form;
	}
}
