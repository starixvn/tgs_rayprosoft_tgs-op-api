#nullable enable

using System.Collections.Immutable;
using System.Numerics;
using Moq;
using NSubstitute;
using NUnit.Framework.Legacy;
using Tgs.PlayerService.Client;
using Tgs.VipLevelService.Clients;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Services.Operations;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Player;

using Client = Tgs.PlayerService.Client;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class PlayerInfoTests
{
	[Test]
	public async Task Check_Player_Status_InBlacklist_Should_Return_False_When_Player_Not_Exist()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var command = new CheckPlayerBannedQuery(
			Guid.NewGuid(),
			"UserName");

		_ = fakePlayerServiceClient
			.GetPlayerInfoByUserNameAsync(
				Arg.Is(command.BrandId),
				Arg.Is(command.UserName))
			.Returns((Client.PlayerInfo?)null);

		var sut = new CheckPlayerBannedQueryService(fakePlayerServiceClient);
		var actual = await sut.GetAsync(command);

		ClassicAssert.IsFalse(actual);
	}

	[Test]
	public async Task Check_Player_Status_InBlacklist_Should_Return_True()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var playerInfo = new Client.PlayerInfo
		{
			PlayerCode = "player1",
			BrandId = Guid.NewGuid(),
			UserName = "UserName"
		};

		var command = new CheckPlayerBannedQuery(
			playerInfo.BrandId,
			playerInfo.UserName);

		_ = fakePlayerServiceClient
			.GetPlayerInfoByUserNameAsync(
				Arg.Is(command.BrandId),
				Arg.Is(command.UserName))
			.Returns(playerInfo);

		_ = fakePlayerServiceClient
			.GetPlayerStatusAsync(Arg.Is(playerInfo.PlayerCode))
			.Returns(PlayerStatus.InBlacklist);

		var sut = new CheckPlayerBannedQueryService(fakePlayerServiceClient);
		var actual = await sut.GetAsync(command);

		ClassicAssert.IsTrue(actual);
	}

	[Test]
	public async Task Get_Player_Status_Should_Success()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new GetPlayerStatusQueryService(fakePlayerServiceClient);

		var query = new GetPlayerStatusQuery("playerCode");

		_ = await sut.GetAsync(query);

		_ = fakePlayerServiceClient.Received(1)
			.GetPlayerStatusAsync(Arg.Is(query.PlayerCode));
	}

	[Test]
	public async Task Query_Player_By_UserName_Should_Success()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new GetPlayerInfoByUserNameQueryService(fakePlayerServiceClient);

		var playerInfo = new Client.PlayerInfo
		{
			PlayerCode = "playerCode",
			BrandId = Guid.NewGuid()
		};

		_ = fakePlayerServiceClient
			.GetPlayerInfoByUserNameAsync(Arg.Any<Guid>(), Arg.Any<string>())
			.Returns(playerInfo);

		var brandId = Guid.NewGuid();
		var username = "username";
		var query = new GetPlayerInfoByUserNameQuery(brandId, username);

		var actual = await sut.GetAsync(query);

		ClassicAssert.AreEqual(playerInfo, actual);
	}

	[Test]
	public async Task Query_Player_Info_Should_Call_Get_Player_Main_Data()
	{
		var moqPlayerServiceClient = new Mock<IPlayerServiceClient>();

		var sut = new GetPlayerMainDataQueryService(moqPlayerServiceClient.Object);

		var playerCode = "abcdef123456";

		var query = new GetPlayerMainDataQuery(playerCode);

		_ = await sut.GetAsync(query).ConfigureAwait(false);

		moqPlayerServiceClient.Verify(
			fake => fake.GetPlayerInfoAsync(
				It.Is<string>(x => x == playerCode),
				It.IsAny<CancellationToken>()),
			Times.Exactly(1));
	}

	[Test]
	public async Task Query_Player_Info_Should_Call_Get_Player_Properties()
	{
		var moqPlayerServiceClient = new Mock<IPlayerServiceClient>();

		var sut = new GetPlayerPropertiesQueryService(moqPlayerServiceClient.Object);

		var playerCode = "abcdef123456";

		var query = new GetPlayerPropertiesQuery(playerCode);

		_ = await sut.GetAsync(query).ConfigureAwait(false);

		moqPlayerServiceClient.Verify(
			fake => fake.GetPlayerPropertiesAsync(
				It.Is<string>(x => x == playerCode),
				It.IsAny<CancellationToken>()),
			Times.Exactly(1));
	}

	[Test]
	public async Task Querying_Player_Information_Should_Obtain_The_Player_VipLevel()
	{
		var moqVipLevelServiceClient = new Mock<IVipLevelServiceClient>();

		var sut = new GetPlayerVipLevelQueryService(moqVipLevelServiceClient.Object);

		var playerCode = "abcdef123456";

		var query = new GetPlayerVipLevelQuery(playerCode);

		_ = await sut.GetAsync(query).ConfigureAwait(false);

		moqVipLevelServiceClient
			.Verify(fake =>
				fake.PlayerVipLevelGetAsync(
					It.Is<string>(x => x == playerCode),
					It.IsAny<CancellationToken>()),
					Times.Exactly(1));
	}

	[Test]
	public async Task Set_Player_Birthdate_Should_Success()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var sut = new SetBirthdateCommandService(fakePlayerServiceClient);

		var command = new SetBirthdateCommand("player1", DateOnly.Parse("2022/4/26"));

		await sut.ExecuteAsync(command);

		_ = fakePlayerServiceClient.Received(1)
			.SetPlayerPropertiesAsync(
				Arg.Is(command.PlayerCode),
				Arg.Is<IImmutableDictionary<PlayerPropertyName, string?>>(
					props => props[PlayerPropertyName.Birthdate] == "2022/04/26"));
	}

	[Test]
	public async Task Set_Player_Facebook_Messenger_Id_Should_Success()
	{
		var moqPlayerServiceClient = new Mock<IPlayerServiceClient>();

		var sut = new SetFacebookMessengerCommandService(moqPlayerServiceClient.Object);

		var player = new Player
		{
			PlayerCode = "abcdef123456",
			Username = "Tester",
			BrandId = Guid.NewGuid(),
			DefaultWalletCurrencyCode = "THB"
		};

		var command = new SetFacebookMessengerCommand(player.PlayerCode, "this_is_messenger_id");

		await sut.ExecuteAsync(command);

		moqPlayerServiceClient.Verify(
			fake => fake.SetPlayerPropertiesAsync(
				It.Is<string>(x => x == player.PlayerCode),
				It.Is<IImmutableDictionary<PlayerPropertyName, string?>>(
					x => x.Count == 1
						&& x.ContainsKey(PlayerPropertyName.FacebookMessenger)
						&& x[PlayerPropertyName.FacebookMessenger] == command.FacebookMessengerId),
				It.Is<string>(x => x == null),
				It.IsAny<CancellationToken>()),
			Times.Exactly(1));
	}

	[Test]
	public async Task Set_Player_Line_Id_Should_Success()
	{
		var moqPlayerServiceClient = new Mock<IPlayerServiceClient>();

		var sut = new SetLineCommandService(moqPlayerServiceClient.Object);

		var player = new Player
		{
			PlayerCode = "abcdef123456",
			Username = "Tester",
			BrandId = Guid.NewGuid(),
			DefaultWalletCurrencyCode = "THB"
		};

		var command = new SetLineCommand(player.PlayerCode, "this_is_line_id");

		await sut.ExecuteAsync(command);

		moqPlayerServiceClient.Verify(
			fake => fake.SetPlayerPropertiesAsync(
				It.Is<string>(x => x == player.PlayerCode),
				It.Is<IImmutableDictionary<PlayerPropertyName, string?>>(
					x => x.Count == 1
						&& x.ContainsKey(PlayerPropertyName.Line)
						&& x[PlayerPropertyName.Line] == command.LineId),
				It.Is<string>(x => x == null),
				It.IsAny<CancellationToken>()),
			Times.Exactly(1));
	}

	[Test]
	public async Task Set_Player_QQ_Number_Should_Success()
	{
		var moqPlayerOperations = new Mock<IPlayerOperations>();

		var sut = new SetQqCommandService(moqPlayerOperations.Object);

		var player = new Player
		{
			PlayerCode = "abcdef123456",
			Username = "Tester",
			BrandId = Guid.NewGuid(),
			DefaultWalletCurrencyCode = "THB"
		};

		var command = new SetQqCommand(player.PlayerCode, "this_is_qq_number");

		_ = moqPlayerOperations
			.Setup(fake => fake.GetPlayerByCodeAsync(
				It.Is<string>(x => x == player.PlayerCode)))
			.Returns(Task.FromResult(player));

		await sut.ExecuteAsync(command);

		moqPlayerOperations
			.Verify(
				fake => fake.SetQqAsync(
					It.Is<string>(x => x == player.PlayerCode),
					It.Is<string>(x => x == command.QqId),
					It.IsAny<DateTimeOffset>()),
				Times.Exactly(1));
	}

	[Test]
	public async Task Set_Player_Telegram_Number_Should_Success()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new SetTelegramCommandService(fakePlayerServiceClient);

		var player = new Player
		{
			PlayerCode = "abcdef123456",
			Username = "Tester",
			BrandId = Guid.NewGuid(),
			DefaultWalletCurrencyCode = "THB"
		};

		var command = new SetTelegramCommand(player.PlayerCode, "this_is_telegram_number");

		await sut.ExecuteAsync(command);

		_ = fakePlayerServiceClient.Received(1)
			.SetPlayerPropertiesAsync(
				Arg.Is(player.PlayerCode),
				Arg.Is<ImmutableDictionary<PlayerPropertyName, string?>>(
					props => props[PlayerPropertyName.Telegram] == command.TelegramId));
	}

	[Test]
	public async Task Set_Player_Viber_Id_Should_Success()
	{
		var moqPlayerServiceClient = new Mock<IPlayerServiceClient>();

		var sut = new SetViberCommandService(moqPlayerServiceClient.Object);

		var player = new Player
		{
			PlayerCode = "abcdef123456",
			Username = "Tester",
			BrandId = Guid.NewGuid(),
			DefaultWalletCurrencyCode = "THB"
		};

		var command = new SetViberCommand(player.PlayerCode, "this_is_viber_id");

		await sut.ExecuteAsync(command);

		moqPlayerServiceClient.Verify(
			fake => fake.SetPlayerPropertiesAsync(
				It.Is<string>(x => x == player.PlayerCode),
				It.Is<IImmutableDictionary<PlayerPropertyName, string?>>(
					x => x.Count == 1
						&& x.ContainsKey(PlayerPropertyName.Viber)
						&& x[PlayerPropertyName.Viber] == command.ViberId),
				It.Is<string>(x => x == null),
				It.IsAny<CancellationToken>()),
			Times.Exactly(1));
	}

	[Test]
	public async Task Set_Player_WeChat_Number_Should_Success()
	{
		var moqPlayerOperations = new Mock<IPlayerOperations>();

		var sut = new SetWeChatCommandService(moqPlayerOperations.Object);

		var player = new Player
		{
			PlayerCode = "abcdef123456",
			Username = "Tester",
			BrandId = Guid.NewGuid(),
			DefaultWalletCurrencyCode = "THB"
		};

		var command = new SetWeChatCommand(player.PlayerCode, "this_is_wechat_number");

		_ = moqPlayerOperations
			.Setup(fake => fake.GetPlayerByCodeAsync(
				It.Is<string>(x => x == player.PlayerCode)))
			.Returns(Task.FromResult(player));

		await sut.ExecuteAsync(command);

		moqPlayerOperations
			.Verify(
				fake => fake.SetWeChatAsync(
					It.Is<string>(x => x == player.PlayerCode),
					It.Is<string>(x => x == command.WeChatId),
					It.IsAny<DateTimeOffset>()),
				Times.Exactly(1));
	}

	[Test]
	public async Task Set_Player_WhatsApp_Id_Should_Success()
	{
		var moqPlayerServiceClient = new Mock<IPlayerServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new SetWhatsAppCommandService(fakePlayerServiceClient);

		var player = new Player
		{
			PlayerCode = "abcdef123456",
			Username = "Tester",
			BrandId = Guid.NewGuid(),
			DefaultWalletCurrencyCode = "THB"
		};

		var command = new SetWhatsAppCommand(player.PlayerCode, "this_is_whatsapp_id");

		await sut.ExecuteAsync(command);

		_ = fakePlayerServiceClient.Received(1)
			.SetPlayerPropertiesAsync(
				Arg.Is(player.PlayerCode),
				Arg.Is<ImmutableDictionary<PlayerPropertyName, string?>>(
					props => props[PlayerPropertyName.WhatsApp] == command.WhatsAppId));
	}

	[Test]
	public async Task Set_Player_Zalo()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new SetZaloCommandService(fakePlayerServiceClient);

		var playerCode = "playerCode";

		var command = new SetZaloCommand(playerCode, "testZalo");

		await sut.ExecuteAsync(command);

		_ = fakePlayerServiceClient.Received(1)
			.SetPlayerPropertiesAsync(
				Arg.Is(playerCode),
				Arg.Is<ImmutableDictionary<PlayerPropertyName, string?>>(
					props => props[PlayerPropertyName.Zalo] == command.Zalo));
	}
}
