using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.BrandPayments;

internal class GetPaymentQueryService(IPaymentServiceClient paymentServiceClient) : IGetService<GetPaymentQuery, PaymentInfo>
{
	private readonly IPaymentServiceClient _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));

	public ValueTask<PaymentInfo> GetAsync(GetPaymentQuery query, CancellationToken cancellationToken = default)
		=> _paymentServiceClient.GetPaymentByIdAsync(query.PaymentId, cancellationToken)!;
}