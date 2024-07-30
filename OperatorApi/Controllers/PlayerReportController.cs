using System.ComponentModel;
using System.Globalization;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Tgs.Report.Client;
using Tgs.Report.Client.Models.Round;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Apis.OperatorApi.Models.Messages.ReportPlayerRound;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.PlayerReport;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PlayerReportController(IReportClient client) : ControllerBase
{
	/// <summary>
	/// Get player round report
	/// </summary>
	[HttpGet("rounds", Name = "GetRounds")]
	[Description("Get rounds")]
	[Consumes(MediaTypeNames.Application.Json)]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<PlayerRoundsResponse> GetRoundsAsync(
		[FromServices] IGetService<GetPlayerRoundsQuery, RoundsResponse> playerRoundsService,
		[FromQuery] GetPlayerRoundReportRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var roundsRequest = new GetPlayerRoundsQuery
		{
			BrandId = brandId,
			PlayerCode = playerTokenData!.PlayerCode,
			From = request.From,
			To = request.To,
			CultureName = request.CultureName ?? CultureInfo.CurrentUICulture.Name,
			Page = request.Page,
			Rows = request.Rows,
			OrderBy = (short)(request.IsDesc ? 1 : 0),
			SortBy = request.SortBy,
			ShowAll = request.ShowAll
		};

		var response = await playerRoundsService.GetAsync(roundsRequest, HttpContext.RequestAborted);

		return new PlayerRoundsResponse
		{
			TotalCount = response.TotalCount,
			Items = response.Items,
			Total = response.Total
		};
	}

	/// <summary>
	/// Get the rounds total asynchronous.
	/// </summary>
	/// <param name="request">The request.</param>
	/// <returns></returns>
	[HttpGet("rounds/total", Name = "GetRoundsTotal")]
	[Description("Get rounds total about Bet Amount Sum & Valid Bet Amount Sum & Win Loss Sum")]
	[Consumes(MediaTypeNames.Application.Json)]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<RoundTotalResponse> GetRoundsTotalAsync(
		[FromQuery] GetPlayerRoundTotalReportRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var roundsRequest = new GetRoundsTotalRequest
		{
			BrandId = brandId,
			PlayerCode = playerTokenData.PlayerCode,
			From = request.From,
			To = request.To,
			ClosedFrom = null,
			ClosedTo = null
		};

		var response = await client.GetRoundsTotalAsync(roundsRequest);

		return new RoundTotalResponse
		{
			BetAmount = response.BetAmount ?? 0M,
			ValidBetAmount = response.ValidBetAmount ?? 0M,
			WinLoss = response.WinLoss ?? 0M
		};
	}
}