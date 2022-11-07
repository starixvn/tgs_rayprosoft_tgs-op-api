using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;
using Tgs.Report.Client;
using Tgs.Report.Client.Models;
using Tgs.Report.Client.Models.Round;
using TGS.Apis.OperatorApi.Controllers;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Models;
using TGS.Core.Models.Tokens;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class PlayerReportTests
{
	[Test]
	public async Task Player_Report_Should_Call_Report_Api_Success()
	{
		// arrange
		var fakeReportClient = Substitute.For<IReportClient>();
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

		var roundList = new List<RoundModel>
		{
			new RoundModel()
		};

		var rounds = roundList.ToArray();

		var resp = new PagedResponse<RoundModel>
		{
			Message = string.Empty,
			TotalCount = rounds.Length,
			Items = rounds
		};

		_ = fakeReportClient
			.GetRoundsAsync(Arg.Any<GetRoundsRequest>())
			.Returns(resp);

		var sut = new PlayerReportController(fakeReportClient)
		{
			ControllerContext = new ControllerContext { HttpContext = fakeHttpContext }
		};

		// act
		_ = await sut.GetRoundsAsync(playerRoundRequest);

		// assert
		_ = fakeReportClient.Received(1)
			.GetRoundsAsync(Arg.Any<GetRoundsRequest>());
	}
}
