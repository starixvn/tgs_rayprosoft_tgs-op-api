using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.BrandPayments;

internal class AllPaymentMethodsQueryService : IGetService<AllPaymentMethodsQuery, IDictionary<Guid, PaymentMethodInfo>>
{
	private readonly IPaymentServiceClient _paymentServiceClient;

	public AllPaymentMethodsQueryService(IPaymentServiceClient paymentServiceClient)
	{
		_paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
	}

	public async ValueTask<IDictionary<Guid, PaymentMethodInfo>> GetAsync(AllPaymentMethodsQuery query, CancellationToken cancellationToken = default)
	{
		var allPaymentMethods = await _paymentServiceClient.GetAllPaymentMethodsAsync(cancellationToken)
			.ToDictionaryAsync(method => method.Id, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return new ReadOnlyDictionary<Guid, PaymentMethodInfo>(allPaymentMethods);
	}
}
