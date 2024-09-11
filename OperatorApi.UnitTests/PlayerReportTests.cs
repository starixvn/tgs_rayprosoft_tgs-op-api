using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tgs.Report.Client;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Controllers;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Models;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Tokens;
using Tgs.Report.Client.Models;
using TGS.Services.Operator.ApplicationServices.PlayerReport;
using TGS.Services.Operator;
using RoundTotalModel = TGS.Services.Operator.RoundTotalModel;
using TGS.Services.Operator.GameProviders;
using Tgs.Report.Client.Models.Round;
using RoundModel = Tgs.Report.Client.Models.Round.RoundModel;

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

	[Test]
	public async Task  Get_Player_Rounds_With_Game_Translation()
	{
		// arrange
		var fakeReportClient = Substitute.For<IReportClient>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();

		var playerCode = "playerCode";
		var fakeHttpContext = new DefaultHttpContext();
		var brandId = Guid.NewGuid();
		fakeHttpContext.Items.Add(Consts.ContextItems.BrandId, brandId);
		fakeHttpContext.Items.Add("playerToken", new PlayerTokenData { PlayerCode = playerCode });

		var request = new GetPlayerRoundsQuery
		{
			From = new DateTimeOffset(new DateTime(2021, 4, 19)),
			To = new DateTimeOffset(new DateTime(2021, 5, 19, 23, 59, 59)),
			CultureName = "zh-CN",
			Page = 1,
			Rows = 20,
			SortBy = string.Empty,
			ShowAll = false
		};

		var roundList = new RoundModel[]
		{
			new() {
				GameId = 1,
				GameName = "g1"
			},
			new() {
				GameId = 2,
				GameName = "g2"
			}
		};

		var resp = new PagedResponse<RoundModel>
		{
			Message = string.Empty,
			TotalCount = roundList.Length,
			Items = roundList
		};

		var games = new BrandGameInfo[]
		{
			new(1, "bg1", "bg1", "", "", Guid.NewGuid(), "bg1", GameStatus.Active, Core.Models.Enums.GameType.Slot,
				GamePlatformType.Desktop, true, "",
				"""
				{
				  "en-US": {
				    "name": "Gem Party"
				  },
				  "id-ID": {
				    "name": "Gem Party"
				  },
				  "ja-JP": {
				    "name": ""
				  },
				  "ko-KR": {
				    "name": ""
				  },
				  "th-TH": {
				    "name": "Gem Party"
				  },
				  "vi-VN": {
				    "name": "Gem Party"
				  },
				  "zh-CN": {
				    "name": "宝石派对"
				  },
				  "zh-TW": {
				    "name": "寶石派對"
				  }
				}
				""",
				GameTag.Hot, 1),
			new(2, "bg2", "bg2", "", "", Guid.NewGuid(), "bg2", GameStatus.Active, Core.Models.Enums.GameType.Slot,
				GamePlatformType.Desktop, true, "",
				"",
				GameTag.Hot, 1)
		};

		_ = fakeReportClient
			.GetRoundsAsync(Arg.Any<GetRoundsRequest>())
			.Returns(resp);

		_ = fakeReportClient
			.GetRoundsTotalAsync(Arg.Any<GetRoundsTotalRequest>())
			.Returns(new Tgs.Report.Client.Models.Round.RoundTotalModel());

		_ = fakeGameProviderRepository.QueryAllGamesAsync(Arg.Any<Guid>()).Returns(games.ToAsyncEnumerable());

		var sut = new GetPlayerRoundsQueryService(fakeGameProviderRepository, fakeReportClient);

		// act
		var response = await sut.GetAsync(request, default);

		// assert
		Assert.That(response.TotalCount, Is.EqualTo(2));
		Assert.That(response.Items.First(x => x.GameId == 1).GameName, Is.EqualTo("宝石派对"));
		Assert.That(response.Items.First(x => x.GameId == 2).GameName, Is.EqualTo("g2"));
	}
}