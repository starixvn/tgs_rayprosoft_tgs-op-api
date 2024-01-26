using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Deposit;

internal class GetDepositFormQueryService(IPaymentServiceClient paymentServiceClient) : IGetService<GetDepositFormQuery, DepositFormInfo?>
{
	private readonly IPaymentServiceClient _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));

	public async ValueTask<DepositFormInfo?> GetAsync(GetDepositFormQuery query, CancellationToken cancellationToken = default)
	{
		var formData = await _paymentServiceClient
			.GetDepositFormAsync(query.BrandId, query.FormId, cancellationToken)
			.ConfigureAwait(false);

		return formData != null && formData.PlayerCode == query.PlayerCode ? formData : null;
	}
}
