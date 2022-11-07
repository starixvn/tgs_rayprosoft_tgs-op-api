using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.BrandPayments;

internal class AllPaymentChannelsQueryService : IGetService<AllPaymentChannelsQuery, IDictionary<Guid, PaymentChannelInfo>>
{
	private readonly IPaymentServiceClient _paymentServiceClient;

	public AllPaymentChannelsQueryService(IPaymentServiceClient paymentServiceClient)
	{
		_paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
	}

	public async ValueTask<IDictionary<Guid, PaymentChannelInfo>> GetAsync(AllPaymentChannelsQuery query, CancellationToken cancellationToken = default)
	{
		var allPaymentChannels = await _paymentServiceClient.GetAllPaymentChannelsAsync(cancellationToken)
			.ToDictionaryAsync(method => method.Id, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return new ReadOnlyDictionary<Guid, PaymentChannelInfo>(allPaymentChannels);
	}
}
