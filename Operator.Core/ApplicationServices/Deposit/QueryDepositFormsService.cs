using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Deposit;

internal class QueryDepositFormsService(
	IPaymentServiceClient paymentServiceClient) : IGetService<DepositFormsQuery, PagingResponse<DepositFormInfo>>
{
	private readonly IPaymentServiceClient _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));

	public async ValueTask<PagingResponse<DepositFormInfo>> GetAsync(
		DepositFormsQuery query,
		CancellationToken cancellationToken = default)
	{
		return await _paymentServiceClient.QueryDepositFormsAsync(new DepositFormQueryConditions
		{
			BrandId = query.BrandId,
			UserId = query.PlayerCode,
			CreateOnRange = query.CreateOnRange,
			UpdateOnRange = query.UpdateOnRange,
			Status = query.Status
		}, new Paging
		{
			IsDesc = query.Paging.IsDesc,
			Page = query.Paging.Page,
			Rows = query.Paging.Rows,
			ShowAll = query.Paging.ShowAll,
			SortBy = query.Paging.SortBy
		}, cancellationToken);
	}
}