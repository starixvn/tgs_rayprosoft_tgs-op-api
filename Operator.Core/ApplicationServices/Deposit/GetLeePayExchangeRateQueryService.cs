using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Deposit;

internal class GetLeePayExchangeRateQueryService : IGetService<GetLeePayExchangeRateQuery, string>
{
    private readonly IPaymentClient _paymentClient;

    public GetLeePayExchangeRateQueryService(IPaymentClient paymentClient)
    {
        _paymentClient = paymentClient ?? throw new ArgumentNullException(nameof(paymentClient));
    }

    public ValueTask<string> GetAsync(GetLeePayExchangeRateQuery query, CancellationToken cancellationToken = default)
        => _paymentClient.AskAsync(query.Subject, query.LeePayQueryExchangeRateSettings);
}
