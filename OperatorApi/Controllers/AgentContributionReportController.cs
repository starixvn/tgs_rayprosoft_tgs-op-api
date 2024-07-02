using Microsoft.AspNetCore.Mvc;
using Tgs.Report.Client.Models.Agent;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Apis.OperatorApi.Models.Messages.ReportAgentContribution;
using Tgs.Report.Client.Enum;
using Tgs.Report.Client.Models;
using TGS.Services.Operator.ApplicationServices.Player;
using TGS.Services.Operator.ApplicationServices.Report.AgentContributionReport;

namespace TGS.Apis.OperatorApi.Controllers;
[Route("api/[controller]")]
[ApiController]
public class AgentContributionReportController : ControllerBase
{
	[HttpGet]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<AgentContributionReportResponse> GetAgentContributionsAsync(
		[FromServices] IGetService<GetPlayerAgentQuery, Guid?> playerAgentService,
		[FromServices] IGetService<GetAgentContributionQuery, PagedResponse<AgentContributionModel>> agentContributionQuery,
		[FromQuery] GetContributionsRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var agentId =
			await playerAgentService.GetAsync(new GetPlayerAgentQuery(playerTokenData!.PlayerCode),
				HttpContext.RequestAborted);

		if (agentId is null)
		{
			return new AgentContributionReportResponse();
		}

		var response = await agentContributionQuery.GetAsync(new GetAgentContributionQuery
		{
			BrandId = brandId,
			AgentId = agentId.Value,
			IncludeAgents = true,
			From = request.From,
			To = request.To,
			Page = 1,
			Rows = int.MaxValue,
			OrderBy = request.IsDesc ? (short)SortOrder.Descending : (short)SortOrder.Ascending,
			SortBy = request.SortBy,
			ShowAll = true
		});

		var page = request.Page <= 0 ? 0 : request.Page - 1;
		var pageSize = request.Rows <= 0 ? 10 : request.Rows;

		var items = response.Items.Select(x => new AgentContributionReportModel(
			x.Username,
			x.Type,
			x.DepositAmount,
			x.DepositHandlingFee,
			x.WithdrawalAmount,
			x.CompanyAdjustAmount,
			x.AgentAdjustAmount,
			x.BonusAdjustAmount,
			x.BonusAmount,
			x.BetAmount,
			x.ValidBetAmount,
			x.Turnover,
			x.WinLossAmount,
			x.NetProfit)).Skip(page * pageSize).Take(pageSize);

		var total = new AgentContributionTotalModel(
			response.Items.Sum(x => x.DepositAmount),
			response.Items.Sum(x => x.DepositHandlingFee),
			response.Items.Sum(x => x.WithdrawalAmount),
			response.Items.Sum(x => x.CompanyAdjustAmount),
			response.Items.Sum(x => x.BonusAdjustAmount),
			response.Items.Sum(x => x.AgentAdjustAmount),
			response.Items.Sum(x => x.BonusAmount),
			response.Items.Sum(x => x.BetAmount),
			response.Items.Sum(x => x.ValidBetAmount),
			response.Items.Sum(x => x.Turnover),
			response.Items.Sum(x => x.WinLossAmount),
			response.Items.Sum(x => x.NetProfit));

		return new AgentContributionReportResponse
		{
			TotalCount = response.TotalCount,
			Items = items,
			Total = total
		};
	}
}
