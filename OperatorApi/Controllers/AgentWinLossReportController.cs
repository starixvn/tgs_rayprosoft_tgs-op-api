using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages.ReportAgentWinLoss;
using Tgs.Report.Client.Models.AgentWinLoss;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.AgentWinLossReport;
using TGS.Services.Operator.ApplicationServices.Player;
using AgentPlayerSpecialDetailModel = TGS.Services.Operator.AgentPlayerSpecialDetailModel;
using CompanyTotalDetailModel = TGS.Services.Operator.CompanyTotalDetailModel;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AgentWinLossReportController : ControllerBase
{
	[HttpGet("breadcrumb")]
	[Description("Get Agent WinLoss Breadcrumb")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<IEnumerable<AgentWinLossBreadcrumb>>
		GetBreadcrumbAsync(
			[FromServices] IGetService<GetAgentWinLossBreadcrumbQuery, IEnumerable<AgentWinLossBreadcrumb>>
				agentWinLossBreadcrumbService,
			[FromServices] IGetService<GetPlayerAgentQuery, Guid?> playerAgentService,
			[FromQuery] GetAgentWinLossBreadcrumbReportRequest request)
	{
		var brandId = HttpContext.Items.GetBrandId();
		var playerData = HttpContext.Items.GetPlayerTokenData();

		var agentId =
			await playerAgentService.GetAsync(new GetPlayerAgentQuery(playerData!.PlayerCode),
				HttpContext.RequestAborted);

		if (agentId is null)
		{
			return [];
		}

		var response = await agentWinLossBreadcrumbService
			.GetAsync(new GetAgentWinLossBreadcrumbQuery
			{
				BrandId = brandId,
				AgentId = agentId.Value,
				Username = request.Username,
				MemberType = request.MemberType,
				RoundId = request.RoundId
			},
				HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return response.Select(x => new AgentWinLossBreadcrumb
		{
			Username = x.Username,
			MemberType = x.MemberType,
			Id = x.Id
		});
	}

	[HttpGet("agentWinLoss")]
	[Description("Get Agent WinLoss")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<GetAgentWinLossReportResponse>
		GetAgentWinLossAsync(
			[FromServices] IGetService<GetAgentWinLossQuery, GetAgentWinLossResponse>
				agentWinLossService,
			[FromServices] IGetService<GetPlayerAgentQuery, Guid?> playerAgentService,
			[FromQuery] GetAgentWinLossReportRequest request)
	{
		var brandId = HttpContext.Items.GetBrandId();
		var playerData = HttpContext.Items.GetPlayerTokenData();

		var agentId =
			await playerAgentService.GetAsync(new GetPlayerAgentQuery(playerData!.PlayerCode),
				HttpContext.RequestAborted);

		if (agentId is null)
		{
			return new GetAgentWinLossReportResponse();
		}

		var query = new GetAgentWinLossQuery
		{
			BrandId = brandId,
			AgentId = agentId.Value,
			Username = request.Username,
			MemberType = request.MemberType,
			DownLine = request.DownLine,
			From = request.From,
			To = request.To,
			Page = request.Page,
			Rows = request.Rows,
			OrderBy = request.OrderBy,
			SortBy = request.SortBy,
			ShowAll = request.ShowAll
		};

		var response = await agentWinLossService
			.GetAsync(query, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return new GetAgentWinLossReportResponse
		{
			Items =
				response.Items?.Select(x => new AgentWinLossReportModel
				{
					Id = x.Id,
					Username = x.Username,
					MemberType = x.MemberType,
					RoundCount = x.RoundCount,
					Turnover = x.Turnover,
					WinLoss = x.WinLoss,
					AgentWinLoss = x.AgentWinLoss,
					CompanyWinLoss = x.CompanyWinLoss,
					Special = x.Special,
					AgentSpecial = x.AgentSpecial,
					CompanySpecial = x.CompanySpecial,
					AgentTotal = x.AgentTotal,
					CompanyTotal = x.CompanyTotal
				}).ToArray(),
			Total = new AgentWinLossReportTotalModel
			{
				RoundCount = response.Total?.RoundCount ?? default,
				Turnover = response.Total?.Turnover ?? default,
				WinLoss = response.Total?.WinLoss ?? default,
				AgentWinLoss = response.Total?.AgentWinLoss ?? default,
				CompanyWinLoss = response.Total?.CompanyWinLoss ?? default,
				Special = response.Total?.Special ?? default,
				AgentSpecial = response.Total?.AgentSpecial ?? default,
				CompanySpecial = response.Total?.CompanySpecial ?? default,
				AgentTotal = response.Total?.AgentTotal ?? default,
				CompanyTotal = response.Total?.CompanyTotal ?? default
			},
			TotalCount = response.TotalCount
		};
	}

	[HttpGet("agentPlayerSpecialDetail")]
	[Description("Get Agent Player Special Detail")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<AgentPlayerSpecialDetailReportModel>
		GetAgentPlayerSpecialDetailAsync(
			[FromServices] IGetService<GetAgentPlayerSpecialDetailQuery, AgentPlayerSpecialDetailModel>
				agentPlayerSpecialService,
			[FromQuery] GetAgentPlayerSpecialDetailReportRequest request)
	{
		var brandId = HttpContext.Items.GetBrandId();

		var query = new GetAgentPlayerSpecialDetailQuery
		{
			BrandId = brandId,
			AgentId = request.AgentId,
			From = request.From,
			To = request.To
		};

		var response = await agentPlayerSpecialService
			.GetAsync(query, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return new AgentPlayerSpecialDetailReportModel
		{
			Bonus = response.Bonus,
			BonusAdjust = response.BonusAdjust,
			PlatformCharge = response.PlatformCharge,
			ProductFee = response.ProductFee
		};
	}

	[HttpGet("agentTotalDetail")]
	[Description("Get Agent Total Detail")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<IEnumerable<AgentTotalDetailReportModel>>
		GetAgentTotalDetailAsync(
			[FromServices] IGetService<GetAgentTotalDetailQuery, IEnumerable<AgentTotalDetailModel>>
				agentTotalService,
			[FromQuery] GetAgentTotalDetailReportRequest request)
	{
		var brandId = HttpContext.Items.GetBrandId();

		var query = new GetAgentTotalDetailQuery
		{
			BrandId = brandId,
			AgentId = request.AgentId,
			From = request.From,
			To = request.To
		};

		var response = await agentTotalService
			.GetAsync(query, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return response.Select(x => new AgentTotalDetailReportModel
		{
			From = x.From,
			To = x.To,
			Percentage = x.Percentage,
			PlayerWinLoss = x.PlayerWinLoss,
			PlayerSpecial = x.PlayerSpecial,
			AgentWinLoss = x.AgentWinLoss,
			AgentSpecial = x.AgentSpecial,
			AgentTotal = x.AgentTotal
		});
	}

	[HttpGet("companyTotalDetail")]
	[Description("Get Company Total Detail")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<IEnumerable<CompanyTotalDetailReportModel>>
		GetCompanyTotalDetailAsync(
			[FromServices] IGetService<GetCompanyTotalDetailQuery, IEnumerable<CompanyTotalDetailModel>>
				companyTotalService,
			[FromQuery] GetCompanyTotalDetailReportRequest request)
	{
		var brandId = HttpContext.Items.GetBrandId();

		var query = new GetCompanyTotalDetailQuery
		{
			BrandId = brandId,
			AgentId = request.AgentId,
			From = request.From,
			To = request.To
		};

		var response = await companyTotalService
			.GetAsync(query, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return response.Select(x => new CompanyTotalDetailReportModel
		{
			From = x.From,
			To = x.To,
			Percentage = x.Percentage,
			PlayerWinLoss = x.PlayerWinLoss,
			PlayerSpecial = x.PlayerSpecial,
			CompanyWinLoss = x.CompanyWinLoss,
			CompanySpecial = x.CompanySpecial,
			CompanyTotal = x.CompanyTotal
		});
	}
}