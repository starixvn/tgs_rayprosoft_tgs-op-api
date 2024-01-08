using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Deposit;

internal class GetUBPayExchangeRateQueryService : IGetService<GetUBPayExchangeRateQuery, string>
{
	private readonly IPaymentClient _paymentClient;

	public GetUBPayExchangeRateQueryService(IPaymentClient paymentClient)
	{
		_paymentClient = paymentClient ?? throw new ArgumentNullException(nameof(paymentClient));
	}

	public ValueTask<string> GetAsync(GetUBPayExchangeRateQuery query, CancellationToken cancellationToken = default)
		=> _paymentClient.AskAsync(query.Subject, query.UBPayQueryExchangeRateSettings, cancellationToken);
}
