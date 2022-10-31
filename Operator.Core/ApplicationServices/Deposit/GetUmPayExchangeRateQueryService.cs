using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Deposit;

internal class GetUmPayExchangeRateQueryService : IGetService<GetUmPayExchangeRateQuery, string>
{
	private readonly IPaymentClient _paymentClient;

	public GetUmPayExchangeRateQueryService(IPaymentClient paymentClient)
	{
		_paymentClient = paymentClient ?? throw new ArgumentNullException(nameof(paymentClient));
	}

	public ValueTask<string> GetAsync(GetUmPayExchangeRateQuery query, CancellationToken cancellationToken = default)
	=> _paymentClient.AskAsync(query.Subject, query.UmPayQueryExchangeRateSettings, cancellationToken);
}
