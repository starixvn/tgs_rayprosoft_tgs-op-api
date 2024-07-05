using Microsoft.AspNetCore.Mvc;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages.ReportPlayerAllAgent;
using Tgs.Report.Client;
using Tgs.Report.Client.Enum;
using Tgs.Report.Client.Models.Player;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PlayerAllAgentReportController : ControllerBase
{
	[HttpGet]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<PlayerAllAgentRebateReportResponse> GetPlayerAllAgentRebatesAsync(
		[FromServices] IReportPlayerAllAgentClient playerAllAgentClient,
		GetPlayerAllAgentRebatesReportRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var reportRequest = new GetPlayerAllAgentRebatesRequest
		{
			BrandId = brandId,
			PlayerCode = playerTokenData!.PlayerCode,
			From = request.From,
			To = request.To,
			Page = request.Page,
			Rows = request.Rows,
			OrderBy = request.IsDesc ? (short)SortOrder.Descending : (short)SortOrder.Ascending,
			SortBy = request.SortBy,
			ShowAll = request.ShowAll
		};

		var playerAllAgentRebates = playerAllAgentClient
			.GetPlayerAllAgentRebatesAsync(reportRequest)
			.Select(x => new PlayerAllAgentRebateReportModel
			{
				MaxActivePlayerNumber = x.MaxActivePlayerNumber,
				ValidBet = x.ValidBet,
				Rebate = x.Rebate,
				SettleDateTime = x.SettleDateTime,
				SentOn = x.SentOn,
				From = x.From,
				To = x.To
			});

		var totalRequest = new GetPlayerAllAgentRebateTotalAmountRequest
		{
			BrandId = brandId,
			PlayerCode = playerTokenData!.PlayerCode,
			From = request.From,
			To = request.To
		};

		var total = await playerAllAgentClient
			.GetPlayerAllAgentRebatesTotalAmountAsync(totalRequest);

		return new PlayerAllAgentRebateReportResponse
		{
			Items = playerAllAgentRebates.ToEnumerable(),
			Total = new PlayerAllAgentRebateTotalAmountReportModel
			{
				ValidBet = total.ValidBet, Rebate = total.Rebate
			},
			TotalCount = total.TotalCount
		};
	}
}