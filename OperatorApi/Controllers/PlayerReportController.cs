using System.ComponentModel;
using System.Data.SqlClient;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Tgs.Report.Client;
using Tgs.Report.Client.Models.Round;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;

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
	public async Task<PagingResponse<RoundModel>> GetRoundsAsync(
		[FromQuery] GetPlayerRoundReportRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var roundsRequest = new GetRoundsRequest
		{
			BrandId = brandId,
			PlayerCode = playerTokenData.PlayerCode,
			From = request.From,
			To = request.To,
			ClosedFrom = null,
			ClosedTo = null,
			Page = request.Page,
			Rows = request.Rows,
			OrderBy = (short)(request.IsDesc ? 1 : 0),
			SortBy = request.SortBy,
			ShowAll = request.ShowAll
		};

		var response = await client.GetRoundsAsync(roundsRequest);

		return new PagingResponse<RoundModel>
		{
			TotalCount = response.TotalCount,
			Items = response.Items ?? []
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
	public async Task<RoundTotalModel> GetRoundsTotalAsync(
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

		return new RoundTotalModel
		{
			BetAmount = response.BetAmount,
			ValidBetAmount = response.ValidBetAmount,
			WinLoss = response.WinLoss
		};
	}
}
