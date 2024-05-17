using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.PlayerPayment;

internal class GetPlayerPaymentOverviewQueryService(IPaymentServiceClient paymentServiceClient) : IGetService<GetPlayerPaymentOverviewQuery, PlayerPaymentOverview>
{
    private readonly IPaymentServiceClient _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));

    public async ValueTask<PlayerPaymentOverview> GetAsync(GetPlayerPaymentOverviewQuery query, CancellationToken cancellationToken = default)
        => await _paymentServiceClient
            .GetPlayerPaymentOverviewAsync(query.PlayerCode, cancellationToken)
            .AsTask()
            .ToAsyncEnumerable()
            .Select(overview => new PlayerPaymentOverview(
                query.PlayerCode,
                overview.FirstDepositAmount,
                overview.FirstDepositHandlingFee,
                overview.FirstDepositOn,
                overview.FirstWithdrawAmount,
                overview.FirstWithdrawHandlingFee,
                overview.FirstWithdrawOn,
                overview.RecordedBy,
                overview.TotalDepositAmount,
                overview.TotalWithdrawAmount,
                overview.TotalDepositCount,
                overview.TotalWithdrawCount))
            .FirstAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
}
