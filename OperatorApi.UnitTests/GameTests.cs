using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;
using Tgs.GameProviders.Pluggable.Client;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Controllers;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Tokens;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Game;
using TGS.Services.Operator.ApplicationServices.GameBalance;
using TGS.Services.Operator.ApplicationServices.Wallet;
using TGS.Services.Operator.GameProviders;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class GameTests
{
	[Test]
	public async Task Can_Count_Player_Favorite_Games()
	{
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();

		var sut = new CountPlayerFavoriteGameQueryService(fakeGameProviderRepository);

		var query = new CountPlayerFavoriteGameQuery(
			Guid.NewGuid(),
			"ThisIsPlayerCode");

		_ = await sut.GetAsync(query);

		_ = fakeGameProviderRepository.Received(1)
			.QueryPlayerFavoriteGamesAsync(
				Arg.Is(query.BrandId),
				Arg.Is(query.PlayerCode));
	}

	[Test]
	public async Task Enter_Game_If_Flag_Is_False_Not_FundOut_Wallet_Amount()
	{
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGetPlayerBalanceService = Substitute.For<IGetService<GetBalanceQuery, BalanceInfo>>();
		var fakeFundOutService = Substitute.For<ICommandService<FundOutGameBalanceCommand>>();

		var sut = new FundOutBeforeEnterGameCommandService(
			fakePlayerRepository,
			fakeGetPlayerBalanceService,
			fakeFundOutService);

		var command = new FundOutBeforeEnterGameCommand(
			Guid.NewGuid(),
			"ThisIsPlayerCode",
			"gp1");

		_ = fakePlayerRepository.GetPlayerAutoTransferFlagAsync(Arg.Is(command.PlayerCode))
			.Returns(false);

		_ = fakeGetPlayerBalanceService.GetAsync(Arg.Any<GetBalanceQuery>())
			.Returns(new BalanceInfo(1000M));

		await sut.ExecuteAsync(command);

		_ = fakeFundOutService.Received(0)
			.ExecuteAsync(Arg.Any<FundOutGameBalanceCommand>());
	}

	[Test]
	public async Task Enter_Game_If_Flag_Is_True_Should_FundOut_Wallet_Amount()
	{
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeGetPlayerBalanceService = Substitute.For<IGetService<GetBalanceQuery, BalanceInfo>>();
		var fakeFundOutService = Substitute.For<ICommandService<FundOutGameBalanceCommand>>();

		var sut = new FundOutBeforeEnterGameCommandService(
			fakePlayerRepository,
			fakeGetPlayerBalanceService,
			fakeFundOutService);

		var command = new FundOutBeforeEnterGameCommand(
			Guid.NewGuid(),
			"ThisIsPlayerCode",
			"gp1");

		_ = fakePlayerRepository.GetPlayerAutoTransferFlagAsync(Arg.Is(command.PlayerCode))
			.Returns(true);

		_ = fakeGetPlayerBalanceService.GetAsync(Arg.Any<GetBalanceQuery>())
			.Returns(new BalanceInfo(1000M));

		await sut.ExecuteAsync(command);

		_ = fakeFundOutService.Received(1)
			.ExecuteAsync(Arg.Is<FundOutGameBalanceCommand>(
				cmd => cmd.PlayerCode == command.PlayerCode
					&& cmd.Amount == 1000));
	}

	[Test]
	public async Task FundInAllGameBalance_Should_Call_All_GP_FundIn()
	{
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeGetGameBalanceService = Substitute.For<IGetService<GetGameBalanceQuery, decimal>>();
		var fakeGameFundInService = Substitute.For<ICommandService<FundInGameBalanceCommand>>();

		var sut = new FundInAllGameBalanceCommandService(
			fakeGameProviderRepository,
			fakeGetGameBalanceService,
			fakeGameFundInService);

		var command = new FundInAllGameBalanceCommand(
			Guid.NewGuid(),
			"ThisIsPlayerCode",
			"gp2");

		_ = fakeGameProviderRepository.QueryAllSubGameProvidersAsync(Arg.Is(command.BrandId))
			.Returns(new[]
			{
				new BrandSubGameProviderInfo(
					"sub1",
					"sub1",
					"sub1",
					Guid.NewGuid(),
					"gp1",
					true,
					Status.Active,
					"{}"),
				new BrandSubGameProviderInfo(
					"sub2",
					"sub2",
					"sub2",
					Guid.NewGuid(),
					"gp1",
					true,
					Status.Active,
					"{}"),
				new BrandSubGameProviderInfo(
					"sub3",
					"sub3",
					"sub3",
					Guid.NewGuid(),
					"gp2",
					true,
					Status.Active,
					"{}")
			}.ToAsyncEnumerable());

		_ = fakeGetGameBalanceService.GetAsync(Arg.Any<GetGameBalanceQuery>())
			.Returns(100M);

		await sut.ExecuteAsync(command);

		_ = fakeGameFundInService.Received(1)
			.ExecuteAsync(Arg.Any<FundInGameBalanceCommand>());
	}

	[Test]
	public async Task FundInAllGameBalance_Should_Call_GP_FundIn_When_Amount_Equals_One_Dollar()
	{
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeGetGameBalanceService = Substitute.For<IGetService<GetGameBalanceQuery, decimal>>();
		var fakeGameFundInService = Substitute.For<ICommandService<FundInGameBalanceCommand>>();

		var sut = new FundInAllGameBalanceCommandService(
			fakeGameProviderRepository,
			fakeGetGameBalanceService,
			fakeGameFundInService);

		var command = new FundInAllGameBalanceCommand(
			Guid.NewGuid(),
			"ThisIsPlayerCode",
			"GpCode");

		_ = fakeGameProviderRepository.QueryAllSubGameProvidersAsync(Arg.Is(command.BrandId))
			.Returns(new[]
			{
				new BrandSubGameProviderInfo(
					"sub1",
					"sub1",
					"sub1",
					Guid.NewGuid(),
					"gp1",
					true,
					Status.Active,
					"{}"),
				new BrandSubGameProviderInfo(
					"sub2",
					"sub2",
					"sub2",
					Guid.NewGuid(),
					"gp1",
					true,
					Status.Active,
					"{}"),
				new BrandSubGameProviderInfo(
					"sub3",
					"sub3",
					"sub3",
					Guid.NewGuid(),
					"gp2",
					true,
					Status.Active,
					"{}")
			}.ToAsyncEnumerable());

		_ = fakeGetGameBalanceService.GetAsync(Arg.Any<GetGameBalanceQuery>())
			.Returns(1m);

		await sut.ExecuteAsync(command);

		_ = fakeGameFundInService.Received(2)
			.ExecuteAsync(Arg.Any<FundInGameBalanceCommand>());
	}

	[Test]
	public async Task FundInAllGameBalance_Should_Not_Call_GP_FundIn_When_Amount_LessThan_One_Dollar()
	{
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeGetGameBalanceService = Substitute.For<IGetService<GetGameBalanceQuery, decimal>>();
		var fakeGameFundInService = Substitute.For<ICommandService<FundInGameBalanceCommand>>();

		var sut = new FundInAllGameBalanceCommandService(
			fakeGameProviderRepository,
			fakeGetGameBalanceService,
			fakeGameFundInService);

		var command = new FundInAllGameBalanceCommand(
			Guid.NewGuid(),
			"ThisIsPlayerCode",
			"GpCode");

		_ = fakeGameProviderRepository.QueryAllSubGameProvidersAsync(Arg.Is(command.BrandId))
			.Returns(new[]
			{
				new BrandSubGameProviderInfo(
					"sub1",
					"sub1",
					"sub1",
					Guid.NewGuid(),
					"gp1",
					true,
					Status.Active,
					"{}"),
				new BrandSubGameProviderInfo(
					"sub2",
					"sub2",
					"sub2",
					Guid.NewGuid(),
					"gp1",
					true,
					Status.Active,
					"{}"),
				new BrandSubGameProviderInfo(
					"sub3",
					"sub3",
					"sub3",
					Guid.NewGuid(),
					"gp2",
					true,
					Status.Active,
					"{}")
			}.ToAsyncEnumerable());

		_ = fakeGetGameBalanceService.GetAsync(Arg.Any<GetGameBalanceQuery>())
			.Returns(0.88m);

		await sut.ExecuteAsync(command);

		_ = fakeGameFundInService.Received(0)
			.ExecuteAsync(Arg.Any<FundInGameBalanceCommand>());
	}

	[Test]
	public async Task FundInAllGameBalance_Without_Ignore_Should_Call_All_GP_FundIn()
	{
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeGetGameBalanceService = Substitute.For<IGetService<GetGameBalanceQuery, decimal>>();
		var fakeGameFundInService = Substitute.For<ICommandService<FundInGameBalanceCommand>>();

		var sut = new FundInAllGameBalanceCommandService(
			fakeGameProviderRepository,
			fakeGetGameBalanceService,
			fakeGameFundInService);

		var command = new FundInAllGameBalanceCommand(
			Guid.NewGuid(),
			"ThisIsPlayerCode",
			null);

		_ = fakeGameProviderRepository
			.QueryAllSubGameProvidersAsync(Arg.Is(command.BrandId))
			.Returns(new[]
			{
				new BrandSubGameProviderInfo(
					"sub1",
					"sub1",
					"sub1",
					Guid.NewGuid(),
					"gp1",
					true,
					Status.Active,
					"{}"),
				new BrandSubGameProviderInfo(
					"sub2",
					"sub2",
					"sub2",
					Guid.NewGuid(),
					"gp3",
					true,
					Status.Active,
					"{}"),
				new BrandSubGameProviderInfo(
					"sub3",
					"sub3",
					"sub3",
					Guid.NewGuid(),
					"gp2",
					true,
					Status.Active,
					"{}")
			}.ToAsyncEnumerable());

		_ = fakeGetGameBalanceService.GetAsync(Arg.Any<GetGameBalanceQuery>())
			.Returns(100M);

		await sut.ExecuteAsync(command);

		_ = fakeGameFundInService.Received(3)
			.ExecuteAsync(Arg.Any<FundInGameBalanceCommand>());
	}

	[Test]
	public async Task Get_Game_List_If_System_Game_Provider_Is_Inactive()
	{
		// arrange
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();

		var brandId = Guid.NewGuid();

		var evoGp = new GameProviderDataInfo(Guid.NewGuid(), "EVO", Status.Inactive, false);
		var sboGp = new GameProviderDataInfo(Guid.NewGuid(), "SBO", Status.Active, false);

		var subGameProviders = new[]
		{
			new BrandSubGameProviderInfo(
				"EVO",
				"EVO",
				"EVO",
				evoGp.Id,
				evoGp.Code,
				evoGp.IsTransferWallet,
				evoGp.Status,
				"{}"),
			new BrandSubGameProviderInfo(
				"NT",
				"NT",
				"NT",
				evoGp.Id,
				evoGp.Code,
				evoGp.IsTransferWallet,
				evoGp.Status,
				"{}"),
			new BrandSubGameProviderInfo(
				"RT",
				"RT",
				"RT",
				evoGp.Id,
				evoGp.Code,
				evoGp.IsTransferWallet,
				evoGp.Status,
				"{}"),
			new BrandSubGameProviderInfo(
				"SBO",
				"SBO",
				"SBO",
				sboGp.Id,
				sboGp.Code,
				evoGp.IsTransferWallet,
				sboGp.Status,
				"{}")
		};

		var games = new[]
		{
			new BrandGameInfo(
				1,
				"1",
				"1",
				"test",
				"EVO",
				evoGp.Id,
				"EVO",
				GameStatus.Active,
				GameType.LiveDealer,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0),
			new BrandGameInfo(
				2,
				"2",
				"2",
				"test",
				"RT",
				evoGp.Id,
				"EVO",
				GameStatus.Active,
				GameType.Slot,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0),
			new BrandGameInfo(
				3,
				"3",
				"3",
				"test",
				"NT",
				evoGp.Id,
				"EVO",
				GameStatus.Active,
				GameType.Slot,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0),
			new BrandGameInfo(
				4,
				"4",
				"4",
				"test",
				"SBO",
				sboGp.Id,
				"SBO",
				GameStatus.Active,
				GameType.Sportsbook,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0)
		};

		_ = fakeGameProviderRepository.QueryAllSubGameProvidersAsync(Arg.Is(brandId))
			.Returns(subGameProviders.Where(sgp => sgp.Status != Status.Inactive).ToAsyncEnumerable());

		_ = fakeGameProviderRepository.QueryAllGamesAsync(Arg.Is(brandId))
				.Returns(games.ToAsyncEnumerable());

		var sut = new GetAllGamesByGameTypeQueryService(
			fakeGameProviderRepository);

		var query = new GetAllGamesByGameTypeQuery(
			brandId,
			PlayerType.RealPlayer,
			PlatformType.Desktop,
			Array.Empty<GameType>());

		// act
		var result = await sut.QueryAsync(query).ToArrayAsync();

		// assert
		var expectedSubGpCodes = subGameProviders.Where(sgp => sgp.Status == Status.Active)
			.Select(sgp => sgp.Code).Distinct().ToArray();

		var actualSubGpCodes = result.Select(g => g.GameProviderCode).Distinct().ToArray();

		Assert.That(actualSubGpCodes, Has.All.Matches<string>(s => expectedSubGpCodes.Contains(s)));
	}

	[Test]
	public async Task Get_Game_List_Of_All_GameProviders()
	{
		// arrange
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();

		var brandId = Guid.NewGuid();

		var evoGp = new GameProviderDataInfo(Guid.NewGuid(), "EVO", Status.Active, false);
		var sboGp = new GameProviderDataInfo(Guid.NewGuid(), "SBO", Status.Active, false);

		var subGameProviders = new[]
		{
			new BrandSubGameProviderInfo(
				"EVO",
				"EVO",
				"EVO",
				evoGp.Id,
				evoGp.Code,
				evoGp.IsTransferWallet,
				evoGp.Status,
				"{}"),
			new BrandSubGameProviderInfo(
				"NT",
				"NT",
				"NT",
				evoGp.Id,
				evoGp.Code,
				evoGp.IsTransferWallet,
				evoGp.Status,
				"{}"),
			new BrandSubGameProviderInfo(
				"RT",
				"RT",
				"RT",
				evoGp.Id,
				evoGp.Code,
				evoGp.IsTransferWallet,
				evoGp.Status,
				"{}"),
			new BrandSubGameProviderInfo(
				"SBO",
				"SBO",
				"SBO",
				sboGp.Id,
				sboGp.Code,
				sboGp.IsTransferWallet,
				sboGp.Status,
				"{}")
		};

		var games = new[]
		{
			new BrandGameInfo(
				1,
				"1",
				"1",
				"test",
				"EVO",
				evoGp.Id,
				"EVO",
				GameStatus.Active,
				GameType.LiveDealer,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0),
			new BrandGameInfo(
				2,
				"2",
				"2",
				"test",
				"RT",
				evoGp.Id,
				"EVO",
				GameStatus.Active,
				GameType.Slot,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0),
			new BrandGameInfo(
				3,
				"3",
				"3",
				"test",
				"NT",
				evoGp.Id,
				"EVO",
				GameStatus.Active,
				GameType.Slot,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0),
			new BrandGameInfo(
				4,
				"4",
				"4",
				"test",
				"SBO",
				sboGp.Id,
				"SBO",
				GameStatus.Active,
				GameType.Sportsbook,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0)
		};

		_ = fakeGameProviderRepository.QueryAllSubGameProvidersAsync(Arg.Is(brandId))
			  .Returns(subGameProviders.ToAsyncEnumerable());

		_ = fakeGameProviderRepository.QueryAllGamesAsync(Arg.Is(brandId))
			  .Returns(games.ToAsyncEnumerable());

		var query = new GetAllGamesByGameTypeQuery(
			brandId,
			PlayerType.RealPlayer,
			PlatformType.Desktop,
			Array.Empty<GameType>());

		var sut = new GetAllGamesByGameTypeQueryService(
			fakeGameProviderRepository);

		// act
		var result = await sut.QueryAsync(query).ToArrayAsync();

		// assert
		Assert.AreEqual(games.Length, result.Length);
	}

	[Test]
	public async Task Get_Game_List_Of_Specific_GameProvider()
	{
		// arrange
		var fakeQueryAllService = Substitute.For<IQueryService<GetAllGamesByGameTypeQuery, GameInfo>>();

		var sut = new GetGamesQueryService(fakeQueryAllService);

		var query = new GetGamesQuery(
			Guid.NewGuid(),
			"Test",
			PlayerType.RealPlayer,
			PlatformType.Desktop,
			Array.Empty<GameType>());

		_ = fakeQueryAllService.QueryAsync(Arg.Any<GetAllGamesByGameTypeQuery>())
			.Returns(new[]
			{
				new GameInfo
				{
					Id = 1,
					Code = "test",
					Name = "test",
					GameProviderCode = "Test"
				},
				new GameInfo
				{
					Id = 2,
					Code = "test",
					Name = "test",
					GameProviderCode = "ABC"
				}
			}.ToAsyncEnumerable());

		// act
		var result = await sut.QueryAsync(query).ToArrayAsync();

		// assert
		Assert.AreEqual(1, result.Length);
	}

	[Test]
	public async Task Get_Game_List_Of_Specific_GameTypes()
	{
		// arrange
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();

		var brandId = Guid.NewGuid();

		var evoGp = new GameProviderDataInfo(Guid.NewGuid(), "EVO", Status.Active, false);
		var sboGp = new GameProviderDataInfo(Guid.NewGuid(), "SBO", Status.Active, false);

		var subGameProviders = new[]
		{
			new BrandSubGameProviderInfo(
				"EVO",
				"EVO",
				"EVO",
				evoGp.Id,
				evoGp.Code,
				evoGp.IsTransferWallet,
				evoGp.Status,
				"{}"),
			new BrandSubGameProviderInfo(
				"NT",
				"NT",
				"NT",
				evoGp.Id,
				evoGp.Code,
				evoGp.IsTransferWallet,
				evoGp.Status,
				"{}"),
			new BrandSubGameProviderInfo(
				"RT",
				"RT",
				"RT",
				evoGp.Id,
				evoGp.Code,
				evoGp.IsTransferWallet,
				evoGp.Status,
				"{}"),
			new BrandSubGameProviderInfo(
				"SBO",
				"SBO",
				"SBO",
				sboGp.Id,
				sboGp.Code,
				sboGp.IsTransferWallet,
				sboGp.Status,
				"{}")
		};

		var games = new[]
		{
			new BrandGameInfo(
				1,
				"1",
				"1",
				"test",
				"EVO",
				evoGp.Id,
				"EVO",
				GameStatus.Active,
				GameType.LiveDealer,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0),
			new BrandGameInfo(
				2,
				"2",
				"2",
				"test",
				"RT",
				evoGp.Id,
				"EVO",
				GameStatus.Active,
				GameType.Slot,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0),
			new BrandGameInfo(
				3,
				"3",
				"3",
				"test",
				"NT",
				evoGp.Id,
				"EVO",
				GameStatus.Active,
				GameType.Slot,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0),
			new BrandGameInfo(
				4,
				"4",
				"4",
				"test",
				"SBO",
				sboGp.Id,
				"SBO",
				GameStatus.Active,
				GameType.Sportsbook,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0)
		};

		_ = fakeGameProviderRepository.QueryAllSubGameProvidersAsync(Arg.Is(brandId))
			.Returns(subGameProviders.ToAsyncEnumerable());

		_ = fakeGameProviderRepository.QueryAllGamesAsync(Arg.Is(brandId))
			.Returns(games.ToAsyncEnumerable());

		var sut = new GetAllGamesByGameTypeQueryService(
			fakeGameProviderRepository);

		var gameTypes = new[]
		{
			GameType.Slot
		};

		var query = new GetAllGamesByGameTypeQuery(
			brandId,
			PlayerType.RealPlayer,
			PlatformType.Desktop,
			gameTypes);

		// act
		var result = await sut.QueryAsync(query).ToArrayAsync();

		// assert
		var expected = games.Where(g => gameTypes.Contains(g.Type)).ToArray();

		Assert.AreEqual(expected.Length, result.Length);
		Assert.AreNotEqual(games.Length, result.Length);
	}

	[Test]
	public async Task Get_Game_Url_With_Extend_Data()
	{
		// arrange
		var brandId = Guid.NewGuid();
		var playerCode = "playerCode";
		var gameUrl = new Uri("http://www.fakeurl.com");

		var backUrl = new Uri("http://www.lobbyurl.com");
		var extendedData = JsonDocument.Parse(@$"{{""lobbyurl"":""{backUrl.AbsoluteUri}""}}");

		var playerToken = new PlayerTokenData
		{
			PlayerCode = playerCode,
			ExpiredOn = new DateTimeOffset(new DateTime(2021, 5, 20)),
			LoginRecordId = Guid.NewGuid(),
			PlatformType = PlatformType.Desktop
		};

		var gameProviderId = new Guid("fc041f82-357f-c4b4-8e37-c2d1701957ee");

		var games = new[]
		{
			new BrandGameInfo(
				1,
				"1",
				"1",
				"test",
				"EVO",
				gameProviderId,
				"EVO",
				GameStatus.Active,
				GameType.LiveDealer,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0),
			new BrandGameInfo(
				2,
				"2",
				"2",
				"test",
				"RT",
				gameProviderId,
				"EVO",
				GameStatus.Active,
				GameType.Slot,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0),
			new BrandGameInfo(
				3,
				"3",
				"3",
				"test",
				"NT",
				gameProviderId,
				"EVO",
				GameStatus.Active,
				GameType.Slot,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0)
		};

		var query = new GetGameUrlQuery(
			brandId,
			playerToken,
			2,
			"zh-CN",
			"127.0.0.1",
			extendedData);

		var fakePluggableClient = Substitute.For<IGameProvidersOperatorPluggableClient>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();

		_ = fakeGameProviderRepository.QueryAllGamesAsync(query.BrandId)
			.Returns(games.ToAsyncEnumerable());

		_ = fakePluggableClient
			.GetGameUrlAsync(
				playerToken.PlayerCode,
				playerToken.LoginRecordId,
				playerToken.PlatformType == PlatformType.Mobile,
				query.GameId,
				query.LanguageCode,
				query.IpAddress,
				backUrl)
			.Returns(ValueTask.FromResult(gameUrl));

		var sut = new GetGameUrlQueryService(
			fakePluggableClient,
			fakeGameProviderRepository);

		// act
		var actual = await sut.GetAsync(query);

		// assert
		Assert.AreEqual(gameUrl, actual);
	}

	[Test]
	public async Task Get_Lobby_Game_Url_With_Extend_Data()
	{
		// arrange
		var playerCode = "playerCode";
		var gameType = GameType.CardGameLobby;

		var playerToken = new PlayerTokenData
		{
			PlayerCode = playerCode,
			ExpiredOn = new DateTimeOffset(new DateTime(2021, 5, 20)),
			LoginRecordId = Guid.NewGuid(),
			PlatformType = PlatformType.Desktop
		};

		var gameUrl = new Uri("http://www.lobbyURL.com");

		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();
		var fakeGetGameUrlQueryService = Substitute.For<IGetService<GetGameUrlQuery, Uri>>();

		var gameProviderId = new Guid("a0f49ce0-2c1d-4f50-9fc5-58b610a30bf5");
		var extendedData = JsonDocument.Parse(@"{""lobbyurl"":""http://www.lobbyurl.com""}");

		var query = new GetLobbyGameUrlQuery(
			Guid.NewGuid(),
			gameProviderId,
			playerToken,
			gameType,
			"en-US",
			"127.0.0.1",
			extendedData);

		var games = new[]
		{
			new BrandGameInfo(
				1000001,
				"1",
				"1",
				"test",
				"KY",
				gameProviderId,
				"KY",
				GameStatus.Active,
				GameType.CardGameLobby,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0),
			new BrandGameInfo(
				1000002,
				"2",
				"2",
				"test",
				"KY",
				gameProviderId,
				"KY",
				GameStatus.Active,
				GameType.CardGame,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0),
			new BrandGameInfo(
				1000003,
				"3",
				"3",
				"test",
				"KY",
				gameProviderId,
				"KY",
				GameStatus.Active,
				GameType.CardGame,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0)
		};

		_ = fakeGameProviderRepository
			.QueryAllGamesAsync(query.BrandId)
			.Returns(games.ToAsyncEnumerable());

		_ = fakeGetGameUrlQueryService
			.GetAsync(Arg.Any<GetGameUrlQuery>())
			.Returns(gameUrl);

		var sut = new GetLobbyGameUrlQueryService(
			fakeGameProviderRepository,
			fakeGetGameUrlQueryService);

		// act
		var actual = await sut.GetAsync(query);

		// assert
		Assert.AreEqual(gameUrl, actual);
	}

	[Test]
	public async Task Player_Can_Add_Favorite_Game()
	{
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();

		var sut = new SetFavoriteGameCommandService(fakeGameProviderRepository);

		var command = new SetFavoriteGameCommand(
			Guid.NewGuid(),
			"ThisIsPlayerCode",
			1000);

		_ = fakeGameProviderRepository.QueryAllGamesAsync(Arg.Is(command.BrandId))
			.Returns(new[]
			{
				new BrandGameInfo(
					command.GameId,
					"code1",
					"name1",
					"test",
					"subgp1",
					Guid.NewGuid(),
					"gp1",
					GameStatus.Active,
					GameType.Arcade,
					GamePlatformType.Desktop,
					true,
					string.Empty,
					"{}",
					GameTag.New,
					0)
			}.ToAsyncEnumerable());

		await sut.ExecuteAsync(command);

		_ = fakeGameProviderRepository.Received(1)
			.AddFavoriteGameAsync(Arg.Is(command.BrandId), Arg.Any<FavoriteGameInfo>());
	}

	[Test]
	public void Player_Can_List_Favorite_Games()
	{
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();

		var sut = new ListFavoriteGameIdsQueryService(fakeGameProviderRepository);

		var query = new ListFavoriteGameIdsQuery(
			Guid.NewGuid(),
			"ThisIsPlayerCode",
			GameType.Arcade);

		_ = sut.QueryAsync(query);

		_ = fakeGameProviderRepository.Received(1)
			.QueryPlayerFavoriteGamesAsync(
				Arg.Is(query.BrandId),
				Arg.Is(query.PlayerCode));
	}

	[Test]
	public async Task Player_Can_Remove_Favorite_Game()
	{
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();

		var sut = new RemoveFavoriteGameCommandService(fakeGameProviderRepository);

		var command = new RemoveFavoriteGameCommand("ThisIsPlayerCode", 1000);

		await sut.ExecuteAsync(command);

		_ = fakeGameProviderRepository.Received(1)
			.RemoveFavoriteGameAsync(Arg.Is(command.PlayerCode), Arg.Is(command.GameId));
	}

	[Test]
	public async Task Should_Fund_In_All_Game_Balance_Before_Get_Game_Url()
	{
		var fakeGetGpCodeService = Substitute.For<IGetService<GetGpCodeQuery, BrandSubGameProviderInfo>>();
		var fakeGetGameUrlQueryService = Substitute.For<IGetService<GetGameUrlQuery, Uri>>();
		var fakeFundInAllService = Substitute.For<ICommandService<FundInAllGameBalanceCommand>>();
		var fakeFundService = Substitute.For<ICommandService<FundOutBeforeEnterGameCommand>>();

		var playerTokenData = new PlayerTokenData
		{
			PlayerCode = "playerCode",
			PlatformType = PlatformType.Desktop,
			LoginRecordId = Guid.NewGuid(),
			ExpiredOn = DateTimeOffset.Now
		};

		var gameUrl = new Uri("http://www.google.com");

		var httpContext = new DefaultHttpContext();
		httpContext.Items.Add("playerToken", playerTokenData);
		httpContext.Items.Add("brandId", Guid.NewGuid());

		var sut = new GameController
		{
			ControllerContext = new ControllerContext
			{
				HttpContext = httpContext
			}
		};

		var request = new GetGameUrlRequest
		{
			SubGameProviderCode = "sub1",
			GameId = 10,
			LanguageCode = "zh-cn"
		};

		_ = fakeGetGpCodeService.GetAsync(Arg.Any<GetGpCodeQuery>())
			.Returns(ValueTask.FromResult(new BrandSubGameProviderInfo(
				"sub1",
				"sub1",
				"sub1",
				Guid.NewGuid(),
				"gp1",
				true,
				Status.Active,
				string.Empty)));

		_ = fakeGetGameUrlQueryService.GetAsync(Arg.Any<GetGameUrlQuery>())
			.Returns(ValueTask.FromResult(gameUrl));

		_ = await sut.GetGameUrlAsync(
			fakeGetGpCodeService,
			fakeGetGameUrlQueryService,
			fakeFundInAllService,
			fakeFundService,
			request);

		_ = fakeFundInAllService.Received(1)
			.ExecuteAsync(Arg.Any<FundInAllGameBalanceCommand>());
	}

	[Test]
	public async Task Should_Fund_In_All_Game_Balance_Before_Get_Lobby_Game_Url()
	{
		var fakeGetGpCodeService = Substitute.For<IGetService<GetGpCodeQuery, BrandSubGameProviderInfo>>();
		var fakeGetLobbyGameUrlQueryService = Substitute.For<IGetService<GetLobbyGameUrlQuery, Uri>>();
		var fakeFundInAllService = Substitute.For<ICommandService<FundInAllGameBalanceCommand>>();
		var fakeFundService = Substitute.For<ICommandService<FundOutBeforeEnterGameCommand>>();

		var playerTokenData = new PlayerTokenData
		{
			PlayerCode = "playerCode",
			PlatformType = PlatformType.Desktop,
			LoginRecordId = Guid.NewGuid(),
			ExpiredOn = DateTimeOffset.Now
		};

		var httpContext = new DefaultHttpContext();
		httpContext.Items.Add("playerToken", playerTokenData);
		httpContext.Items.Add("brandId", Guid.NewGuid());

		var sut = new GameController
		{
			ControllerContext = new ControllerContext
			{
				HttpContext = httpContext
			}
		};

		var request = new GetLobbyGameUrlRequest
		{
			SubGameProviderCode = "sub1",
			LanguageCode = "zh-cn"
		};

		_ = fakeGetGpCodeService.GetAsync(Arg.Any<GetGpCodeQuery>())
			.Returns(ValueTask.FromResult(new BrandSubGameProviderInfo(
				"sub1",
				"sub1",
				"sub1",
				Guid.NewGuid(),
				"gp1",
				true,
				Status.Active,
				string.Empty)));

		_ = fakeGetLobbyGameUrlQueryService
			.GetAsync(Arg.Any<GetLobbyGameUrlQuery>())
			.Returns(ValueTask.FromResult(new Uri("http://www.google.com")));

		_ = await sut.GetLobbyGameUrlAsync(
			fakeGetGpCodeService,
			fakeGetLobbyGameUrlQueryService,
			fakeFundInAllService,
			fakeFundService,
			request);

		_ = fakeFundInAllService.Received(1)
			.ExecuteAsync(Arg.Any<FundInAllGameBalanceCommand>());
	}
}
