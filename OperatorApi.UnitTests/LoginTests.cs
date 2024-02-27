#nullable enable

using System.Security.Claims;
using System.Text;
using ExpectedObjects;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework.Legacy;
using Tgs.BrandCertificate.Clients;
using Tgs.Core.Infrastructures.Security;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Exceptions;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Player;
using Valhalla.MessageQueue;
using BrandStatus = Tgs.BrandCertificate.BrandStatus;

using Client = Tgs.PlayerService.Client;

namespace TGS.Tests.OperatorApi.UnitTests;

internal class LoginTests
{
	[Test]
	public async Task Can_Login()
	{
		var fakeBrandRepository = Substitute.For<IBrandRepository>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeJwtTokenHandler = Substitute.For<IJwtTokenHandler>();
		var fakePlayerLoginRecordRepository = Substitute.For<IPlayerLoginRecordRepository>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakePasswordValidateCommandService = Substitute
			.For<IGetService<PlayerPasswordValidateCommand, bool>>();

		var playerInfo = new Client.PlayerInfo
		{
			BrandId = Guid.NewGuid(),
			PlayerCode = "player1",
			UserName = "UserName",
			RegisteredOn = DateTimeOffset.Now.DateTime
		};

		var loginRecordInfo = new LoginRecordInfo(
			Guid.NewGuid(),
			playerInfo.PlayerCode,
			LoginStatus.Enabled,
			PlatformType.Desktop,
			DateTimeOffset.Now,
			DateTimeOffset.Now.AddDays(1));

		var password = "Password";

		var expected = new LoginInfo(
			"player token",
			loginRecordInfo.CreatedOn);

		var command = new LoginCommand(
			playerInfo.BrandId,
			playerInfo.UserName,
			password,
			"127.0.0.1",
			"abc.net",
			loginRecordInfo.PlatformType,
			"");

		_ = fakeBrandServiceClient.GetBrandStatusAsync(
				Arg.Is(playerInfo.BrandId),
				Arg.Any<CancellationToken>())
			.Returns(BrandStatus.Active);

		_ = fakePlayerServiceClient.GetPlayerInfoByUserNameAsync(
				Arg.Is(playerInfo.BrandId),
				Arg.Is(playerInfo.UserName),
				Arg.Any<CancellationToken>())
			.Returns(playerInfo);

		_ = fakePasswordValidateCommandService.GetAsync(
				Arg.Is(new PlayerPasswordValidateCommand(playerInfo.PlayerCode, command.Password)),
				Arg.Any<CancellationToken>())
			.Returns(true);

		_ = fakePlayerLoginRecordRepository.AddPlayerLoginRecordAsync(
				Arg.Is(command.BrandId),
				Arg.Is(playerInfo.PlayerCode),
				Arg.Is(command.PlatformType),
				Arg.Is(command.IpAddress))
			.Returns(loginRecordInfo);

		_ = fakeBrandRepository.GetSecretKeyAsync(
				Arg.Is(playerInfo.BrandId),
				Arg.Any<CancellationToken>())
			.Returns("secret_key");

		_ = fakeJwtTokenHandler.GenerateToken(
				Arg.Any<ClaimsIdentity>(),
				Arg.Any<byte[]>(),
				Arg.Is(command.Site),
				Arg.Any<DateTime>())
			.Returns(expected.PlayerToken);

		var sut = new LoginCommandService(
			fakeJwtTokenHandler,
			fakeBrandRepository,
			fakePlayerLoginRecordRepository,
			fakePlayerServiceClient,
			fakeBrandServiceClient,
			fakePasswordValidateCommandService);

		var actual = await sut.GetAsync(command);

		expected.ToExpectedObject().ShouldEqual(actual);

		_ = fakePlayerServiceClient
			.Received(1)
			.RecordLoginIpAsync(
				Arg.Is(playerInfo.PlayerCode),
				Arg.Is(command.IpAddress),
				Arg.Is(command.HostName),
				Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task Decrypt_Player_Token_Should_Match_Input_Info()
	{
		var fakeBrandRepository = Substitute.For<IBrandRepository>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakePlayerLoginRecordRepository = Substitute.For<IPlayerLoginRecordRepository>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakePasswordValidateCommandService = Substitute
			.For<IGetService<PlayerPasswordValidateCommand, bool>>();

		var tokenHandler = new JwtTokenHandler();

		var playerInfo = new Client.PlayerInfo
		{
			BrandId = Guid.NewGuid(),
			PlayerCode = "player1",
			UserName = "UserName",
			RegisteredOn = DateTimeOffset.Now.DateTime
		};

		var brandSecretKey = "L5SdeH9DuREUjoG3FN9vtkBY4FtKFitI";

		var loginRecordInfo = new LoginRecordInfo(
			Guid.NewGuid(),
			playerInfo.PlayerCode,
			LoginStatus.Enabled,
			PlatformType.Mobile,
			DateTimeOffset.Now,
			DateTimeOffset.Now.AddDays(1));

		var password = "Password";

		var command = new LoginCommand(
			playerInfo.BrandId,
			playerInfo.UserName,
			password,
			"127.0.0.1",
			"abc.net",
			loginRecordInfo.PlatformType,
			"op-api");

		_ = fakeBrandServiceClient.GetBrandStatusAsync(
				Arg.Is(playerInfo.BrandId),
				Arg.Any<CancellationToken>())
			.Returns(BrandStatus.Active);

		_ = fakePlayerServiceClient.GetPlayerInfoByUserNameAsync(
				Arg.Is(playerInfo.BrandId),
				Arg.Is(playerInfo.UserName),
				Arg.Any<CancellationToken>())
			.Returns(playerInfo);

		_ = fakePasswordValidateCommandService.GetAsync(
				Arg.Is(new PlayerPasswordValidateCommand(playerInfo.PlayerCode, command.Password)),
				Arg.Any<CancellationToken>())
			.Returns(true);

		_ = fakePlayerLoginRecordRepository.AddPlayerLoginRecordAsync(
				Arg.Is(command.BrandId),
				Arg.Is(playerInfo.PlayerCode),
				Arg.Is(command.PlatformType),
				Arg.Is(command.IpAddress))
			.Returns(loginRecordInfo);

		_ = fakeBrandRepository.GetSecretKeyAsync(
				Arg.Is(playerInfo.BrandId),
				Arg.Any<CancellationToken>())
			.Returns(brandSecretKey);

		var sut = new LoginCommandService(
			tokenHandler,
			fakeBrandRepository,
			fakePlayerLoginRecordRepository,
			fakePlayerServiceClient,
			fakeBrandServiceClient,
			fakePasswordValidateCommandService);

		var loginInfo = await sut.GetAsync(command);

		var securityToken = tokenHandler.ReadToken(loginInfo.PlayerToken);
		var actual = securityToken.Claims.ToDictionary(c => c.Type, c => c.Value);

		ClassicAssert.AreEqual(loginRecordInfo.PlayerCode, actual["sub"]);
		ClassicAssert.AreEqual(loginRecordInfo.PlatformType.ToString(), actual["platform_type"]);
		ClassicAssert.AreEqual(loginRecordInfo.Id.ToString(), actual["login_id"]);
	}

	[Test]
	public void Login_Throw_Exception_When_Brand_Is_Not_Active()
	{
		var fakeBrandRepository = Substitute.For<IBrandRepository>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeJwtTokenHandler = Substitute.For<IJwtTokenHandler>();
		var fakePlayerLoginRecordRepository = Substitute.For<IPlayerLoginRecordRepository>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakePasswordValidateCommandService = Substitute
			.For<IGetService<PlayerPasswordValidateCommand, bool>>();

		var command = new LoginCommand(
			Guid.NewGuid(),
			"UserName",
			"Password",
			"127.0.0.1",
			"abc.net",
			PlatformType.Desktop,
			"");

		_ = fakeBrandServiceClient.GetBrandStatusAsync(
				Arg.Is(command.BrandId),
				Arg.Any<CancellationToken>())
			.Returns(BrandStatus.Inactive);

		var sut = new LoginCommandService(
			fakeJwtTokenHandler,
			fakeBrandRepository,
			fakePlayerLoginRecordRepository,
			fakePlayerServiceClient,
			fakeBrandServiceClient,
			fakePasswordValidateCommandService);

		_ = Assert.ThrowsAsync<InvalidStatusException>(async () => await sut.GetAsync(command));

		_ = fakePlayerServiceClient
			.Received(0)
			.RecordLoginIpAsync(
				Arg.Any<string>(),
				Arg.Is(command.IpAddress),
				Arg.Is(command.HostName),
				Arg.Any<CancellationToken>());
	}

	[Test]
	public void Login_Throw_Exception_When_Password_Is_Not_Verified()
	{
		var fakeBrandRepository = Substitute.For<IBrandRepository>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeJwtTokenHandler = Substitute.For<IJwtTokenHandler>();
		var fakePlayerLoginRecordRepository = Substitute.For<IPlayerLoginRecordRepository>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakePasswordValidateCommandService = Substitute
			.For<IGetService<PlayerPasswordValidateCommand, bool>>();

		var playerInfo = new Client.PlayerInfo
		{
			BrandId = Guid.NewGuid(),
			PlayerCode = "player1",
			UserName = "UserName",
			RegisteredOn = DateTimeOffset.Now.DateTime
		};

		var password = "Password1";
		var byteSalt = Encoding.UTF8.GetBytes("Salt");
		var bytePassword = Encoding.UTF8.GetBytes("Password");

		var command = new LoginCommand(
			playerInfo.BrandId,
			playerInfo.UserName,
			password,
			"127.0.0.1",
			"abc.net",
			PlatformType.Desktop,
			"");

		_ = fakeBrandServiceClient.GetBrandStatusAsync(
				Arg.Is(playerInfo.BrandId),
				Arg.Any<CancellationToken>())
			.Returns(BrandStatus.Active);

		_ = fakePlayerServiceClient.GetPlayerInfoByUserNameAsync(
				Arg.Is(playerInfo.BrandId),
				Arg.Is(playerInfo.UserName),
				Arg.Any<CancellationToken>())
			.Returns(playerInfo);

		_ = fakePasswordValidateCommandService.GetAsync(
				Arg.Is(new PlayerPasswordValidateCommand(playerInfo.PlayerCode, command.Password)),
				Arg.Any<CancellationToken>())
			.Returns(false);

		var sut = new LoginCommandService(
			fakeJwtTokenHandler,
			fakeBrandRepository,
			fakePlayerLoginRecordRepository,
			fakePlayerServiceClient,
			fakeBrandServiceClient,
			fakePasswordValidateCommandService);

		_ = Assert.ThrowsAsync<InvalidCredentialsException>(async () => await sut.GetAsync(command));

		_ = fakePlayerServiceClient
			.Received(0)
			.RecordLoginIpAsync(
				Arg.Is(playerInfo.PlayerCode),
				Arg.Is(command.IpAddress),
				Arg.Is(command.HostName),
				Arg.Any<CancellationToken>());
	}

	[Test]
	public void Login_Throw_Exception_When_Player_Does_Not_Exist()
	{
		var fakeBrandRepository = Substitute.For<IBrandRepository>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeJwtTokenHandler = Substitute.For<IJwtTokenHandler>();
		var fakePlayerLoginRecordRepository = Substitute.For<IPlayerLoginRecordRepository>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakePasswordValidateCommandService = Substitute
			.For<IGetService<PlayerPasswordValidateCommand, bool>>();

		var command = new LoginCommand(
			Guid.NewGuid(),
			"UserName",
			"Password",
			"127.0.0.1",
			"abc.net",
			PlatformType.Desktop,
			"");

		_ = fakeBrandServiceClient.GetBrandStatusAsync(
				Arg.Is(command.BrandId),
				Arg.Any<CancellationToken>())
			.Returns(BrandStatus.Active);

		_ = fakePlayerServiceClient.GetPlayerInfoByUserNameAsync(
				Arg.Is(command.BrandId),
				Arg.Is(command.UserName),
				Arg.Any<CancellationToken>())
			.Returns((Client.PlayerInfo?)null);

		var sut = new LoginCommandService(
			fakeJwtTokenHandler,
			fakeBrandRepository,
			fakePlayerLoginRecordRepository,
			fakePlayerServiceClient,
			fakeBrandServiceClient,
			fakePasswordValidateCommandService);

		_ = Assert.ThrowsAsync<Services.Operator.PlayerNotFoundException>(async () => await sut.GetAsync(command));

		_ = fakePlayerServiceClient
			.Received(0)
			.RecordLoginIpAsync(
				Arg.Any<string>(),
				Arg.Is(command.IpAddress),
				Arg.Is(command.HostName),
				Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task 發送登入Event()
	{
		var fakeMessageSender = Substitute.For<IMessageSender>();
		var fakeLogger = NullLogger<PublishPlayerLoggedInEventCommandService>.Instance;

		var command = new PublishPlayerLoggedInEventCommand(
			"player1",
			DateTimeOffset.UtcNow);

		var sut = new PublishPlayerLoggedInEventCommandService(
			fakeMessageSender,
			fakeLogger);

		await sut.ExecuteAsync(command);

		_ = fakeMessageSender.Received(1)
			.PublishAsync(
				Arg.Is("tgs.operator.player.logged_in"),
				Arg.Any<byte[]>(),
				Arg.Any<CancellationToken>());
	}
}