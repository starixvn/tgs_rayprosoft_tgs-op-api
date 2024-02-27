using Microsoft.AspNetCore.Mvc;
using Tgs.CashService.Client;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator.ApplicationServices.Wallet;
using TransactionLogResponseViewMode = TGS.Apis.OperatorApi.Models.Messages.PagingResponse<TGS.Apis.OperatorApi.Models.Messages.TransactionLogViewModel>;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TransactionController : ControllerBase
{
	[HttpGet(Name = "QueryTransactionLogs")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<TransactionLogResponseViewMode> QueryTransactionLogsAsync(
		[FromServices] IGetService<GetPlayerTransactionLogsQuery, Tgs.CashService.Client.PagingResponse<TransactionLog>> queryService,
		[FromQuery] TransactionLogQueryRequest model)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var query = new GetPlayerTransactionLogsQuery(
			playerTokenData.PlayerCode,
			model.From,
			model.To,
			model.Page ?? 1,
			model.Rows ?? 10);

		var result = await queryService.GetAsync(
			query,
			HttpContext.RequestAborted).ConfigureAwait(false);

		return new TransactionLogResponseViewMode
		{
			TotalCount = result.TotalCount,
			Items = result.Items
				.Select(log => new TransactionLogViewModel
				{
					Id = log.Id,
					Amount = log.Amount,
					RecordedOn = log.RecordedOn,
					TransactionType = ConvertTransactionTypeToMessageKey(log.TransactionType)
				})
		};
	}

	private static string ConvertTransactionTypeToMessageKey(int type)
		=> type switch
		{
			6 => "Transaction.Promotion",
			4100 => "Transaction.DividendAllAgentRebate",
			3800 => "Transaction.PromotionVipLevelUpgrade",
			3850 => "Transaction.PromotionVipLevelSeason",
			3860 => "Transaction.PromotionVipLevelBirthday",
			4 => "Transaction.Rebate",
			6500 => "Transaction.ReceiveCommit",
			_ => "Unknown"
		};
}