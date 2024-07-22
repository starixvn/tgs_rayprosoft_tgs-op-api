using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tgs.Report.Client;
using Tgs.Report.Client.Models;
using Tgs.Report.Client.Models.Round;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Controllers;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Models;
using TGS.Core.Models.Tokens;
using TGS.Services.Operator.ApplicationServices.PlayerReport;
using TGS.Services.Operator;
using RoundModel = Tgs.Report.Client.Models.Round.RoundModel;
using RoundTotalModel = TGS.Services.Operator.RoundTotalModel;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class PlayerReportTests
{
	[Test]
	public async Task Player_Report_Should_Call_Report_Api_Success()
	{
		// arrange
		var fakeReportClient = Substitute.For<IReportClient>();
		var fakeQuery = Substitute.For<IGetService<GetPlayerRoundsQuery, RoundsResponse>>();

		var playerCode = "playerCode";
		var fakeHttpContext = new DefaultHttpContext();
		var brandId = Guid.NewGuid();
		fakeHttpContext.Items.Add(Consts.ContextItems.BrandId, brandId);
		fakeHttpContext.Items.Add("playerToken", new PlayerTokenData { PlayerCode = playerCode });

		var playerRoundRequest = new GetPlayerRoundReportRequest
		{
			From = new DateTimeOffset(new DateTime(2021, 4, 19)),
			To = new DateTimeOffset(new DateTime(2021, 5, 19, 23, 59, 59)),
			Page = 1,
			Rows = 20,
			IsDesc = true,
			SortBy = string.Empty,
			ShowAll = false
		};

		var roundResponse = new RoundsResponse
		{
			Total = new RoundTotalModel(),
			TotalCount = 1,
			Items = new Services.Operator.RoundModel[] { new Services.Operator.RoundModel() }
		};

		_ = fakeQuery.GetAsync(Arg.Any<GetPlayerRoundsQuery>(), Arg.Any<CancellationToken>()).Returns(roundResponse);

		var sut = new PlayerReportController(fakeReportClient)
		{
			ControllerContext = new ControllerContext { HttpContext = fakeHttpContext }
		};

		// act
		_ = await sut.GetRoundsAsync(fakeQuery, playerRoundRequest);

		// assert
		_ = fakeQuery.Received(1)
			.GetAsync(Arg.Any<GetPlayerRoundsQuery>(), Arg.Any<CancellationToken>());
	}
}