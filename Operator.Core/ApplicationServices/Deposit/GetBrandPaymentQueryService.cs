using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Deposit;

internal class GetBrandPaymentQueryService(IPaymentServiceClient paymentServiceClient) : IGetService<GetBrandPaymentQuery, BrandPaymentInfo>
{
	private readonly IPaymentServiceClient _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));

	public ValueTask<BrandPaymentInfo> GetAsync(GetBrandPaymentQuery query, CancellationToken cancellationToken = default)
		=> _paymentServiceClient.GetBrandPaymentByIdAsync(query.Id, cancellationToken)!;
}