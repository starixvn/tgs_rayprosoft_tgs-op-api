using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Deposit;

internal class GetBankDepositFromQueryService : IGetService<GetBankDepositFormQuery, DepositFormInfo>
{
    private readonly IPaymentServiceClient _paymentServiceClient;

    public GetBankDepositFromQueryService(IPaymentServiceClient paymentServiceClient)
    {
        _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
    }

    public async ValueTask<DepositFormInfo> GetAsync(GetBankDepositFormQuery query, CancellationToken cancellationToken = default)
    {
        var form = await _paymentServiceClient
            .GetDepositFormAsync(query.BrandId, query.FormId)
            .ConfigureAwait(false);

        if (form == null || form.PlayerCode != query.PlayerCode)
            throw new UltraViresException();

        return form;
    }
}
