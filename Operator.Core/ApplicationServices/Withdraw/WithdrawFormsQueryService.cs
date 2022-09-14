using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

internal class WithdrawFormsQueryService : IGetService<WithdrawFormsQuery, PagingResponse<WithdrawFormInfo>>
{
    private readonly IPaymentServiceClient _paymentServiceClient;

    public WithdrawFormsQueryService(
        IPaymentServiceClient paymentServiceClient)
    {
        _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
    }

    public async ValueTask<PagingResponse<WithdrawFormInfo>> GetAsync(
        WithdrawFormsQuery query,
        CancellationToken cancellationToken = default)
    {
        return await _paymentServiceClient.QueryWithdrawFormsAsync(
            new WithdrawFormQueryConditions
            {
                BrandId = query.BrandId,
                UserId = query.PlayerCode,
                UserType = UserType.Player,
                Status = query.Status,
                CreateOnRange = query.CreateOnRange,
                UpdateOnRange = query.UpdateOnRange
            },
            new Tgs.Payment.Clients.Paging
            {
                IsDesc = query.Paging.IsDesc,
                Page = query.Paging.Page,
                Rows = query.Paging.Rows,
                ShowAll = query.Paging.ShowAll,
                SortBy = query.Paging.SortBy
            });
    }
}
