using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Deposit;

internal class QueryDepositFormsService : IGetService<DepositFormsQuery, PagingResponse<DepositFormInfo>>
{
	private readonly IPaymentServiceClient _paymentServiceClient;

	public QueryDepositFormsService(
		IPaymentServiceClient paymentServiceClient)
	{
		_paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
	}

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
		}, new Tgs.Payment.Clients.Paging
		{
			IsDesc = query.Paging.IsDesc,
			Page = query.Paging.Page,
			Rows = query.Paging.Rows,
			ShowAll = query.Paging.ShowAll,
			SortBy = query.Paging.SortBy
		}, cancellationToken);
	}
}
