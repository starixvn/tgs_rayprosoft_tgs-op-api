using System.Runtime.InteropServices;
using Jering.Javascript.NodeJS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tgs.BrandCertificate.Clients;
using Tgs.MessageQueue;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Tokens;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Player;
using Xunit.Abstractions;
using BrandStatus = Tgs.BrandCertificate.BrandStatus;

namespace OperatorApi.IntegrationTests;

public class PlayerTests
{
	private readonly ITestOutputHelper _testOutputHelper;

	public PlayerTests(ITestOutputHelper testOutputHelper)
	{
		_testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
	}

	[Fact]
	public async Task 玩家登入()
	{
		var channelName = Guid.NewGuid().ToString();

		var fakeBrandCertClient = Substitute.For<IBrandCertificateClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakePlayerPasswordValidateCommandService = Substitute.For<IGetService<PlayerPasswordValidateCommand, bool>>();
		var fakePlayerLoginRecordRepository = Substitute.For<IPlayerLoginRecordRepository>();
		var fakeBrandRepository = Substitute.For<IBrandRepository>();
		var fakeValidatePlayerLoginRecordCommandService = Substitute.For<ICommandService<ValidatePlayerLoginRecordCommand>>();
		var fakePlayerTokenParserCommandService = Substitute.For<IGetService<PlayerTokenParserCommand, PlayerTokenData?>>();

		_ = fakeBrandCertClient.GetSystemValidateSecurityKeyAsync(Arg.Any<CancellationToken>())
			.Returns(TestOperatorApplication.TestSecurityKey);

		var brandId = Guid.NewGuid();
		var brandCode = "Test";
		var brandToken = TestOperatorApplication.MakeTestBrandToken(brandId, brandCode);

		_ = fakeBrandRepository.GetSecretKeyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
			.Returns("N2KAnljo5zbLCZDSoUBFJ70ONsy");

		using var web = TestOperatorApplication.Builder
			.UseTestChannelServer(channelName)
			.ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper))
			.ConfigureServices(services => services
				.AddSingleton(fakeBrandCertClient)
				.AddSingleton(fakeBrandServiceClient)
				.AddSingleton(fakePlayerServiceClient)
				.AddSingleton(fakePlayerPasswordValidateCommandService)
				.AddSingleton(fakePlayerLoginRecordRepository)
				.AddSingleton(fakeBrandRepository)
				.AddSingleton(fakeValidatePlayerLoginRecordCommandService)
				.AddSingleton(fakePlayerTokenParserCommandService))
			.Build();

		await web.StartAsync();

		var playerCode = "test1";
		var userName = "test01";
		var password = "password";
		var platformType = PlatformType.Desktop;

		_ = fakeBrandServiceClient.GetBrandStatusAsync(Arg.Is(brandId))
			.Returns((BrandStatus)BrandStatus.Active);

		_ = fakePlayerServiceClient.GetPlayerInfoByUserNameAsync(Arg.Is(brandId), Arg.Is(userName))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				BrandId = brandId,
				UserName = userName,
				PlayerCode = playerCode
			});

		_ = fakePlayerPasswordValidateCommandService.GetAsync(
				Arg.Is<PlayerPasswordValidateCommand>(x =>
					x.PlayerCode == playerCode && x.Password == password))
			.Returns(true);

		var loginRecord = new LoginRecordInfo(
			Guid.NewGuid(),
			playerCode,
			TGS.Core.Models.Enums.LoginStatus.Enabled,
			TGS.Core.Models.Enums.PlatformType.Desktop,
			DateTimeOffset.Now,
			DateTimeOffset.Now.AddDays(1D));

		_ = fakePlayerLoginRecordRepository.AddPlayerLoginRecordAsync(
				Arg.Is(brandId),
				Arg.Is(playerCode),
				Arg.Is(TGS.Core.Models.Enums.PlatformType.Desktop),
				Arg.Any<string>())
			.Returns(loginRecord);

		_ = fakePlayerTokenParserCommandService.GetAsync(
				Arg.Any<PlayerTokenParserCommand>())
			.Returns(new PlayerTokenData
			{
				PlayerCode = playerCode,
				LoginRecordId = loginRecord.Id,
				ExpiredOn = loginRecord.ExpiredOn,
				PlatformType = platformType
			});

		await using var scope = web.Services.CreateAsyncScope();

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, username, password, platformtype) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken);

				let svc = new OperatorService(instance);

				return await svc.playerLogin(username, password, platformtype);
			};
		""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<LoginResponse>(
			nodeModule,
			args: new object?[] { brandToken, userName, password, (int)platformType });

		// Assert
		Assert.NotNull(actual);
		Assert.NotNull(actual.PlayerToken);
	}

	[Fact]
	public async Task 玩家登入後會發送登入Event()
	{
		var channelName = Guid.NewGuid().ToString();

		var fakeBrandCertClient = Substitute.For<IBrandCertificateClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakePlayerPasswordValidateCommandService = Substitute.For<IGetService<PlayerPasswordValidateCommand, bool>>();
		var fakePlayerLoginRecordRepository = Substitute.For<IPlayerLoginRecordRepository>();
		var fakeBrandRepository = Substitute.For<IBrandRepository>();
		var fakeValidatePlayerLoginRecordCommandService = Substitute.For<ICommandService<ValidatePlayerLoginRecordCommand>>();
		var fakePlayerTokenParserCommandService = Substitute.For<IGetService<PlayerTokenParserCommand, PlayerTokenData?>>();

		var fakeMessageSender = Substitute.For<IMessageSender>();

		_ = fakeBrandCertClient.GetSystemValidateSecurityKeyAsync(Arg.Any<CancellationToken>())
			.Returns(TestOperatorApplication.TestSecurityKey);

		var brandId = Guid.NewGuid();
		var brandCode = "Test";
		var brandToken = TestOperatorApplication.MakeTestBrandToken(brandId, brandCode);

		_ = fakeBrandRepository.GetSecretKeyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
			.Returns("N2KAnljo5zbLCZDSoUBFJ70ONsy");

		using var web = TestOperatorApplication.Builder
			.UseTestChannelServer(channelName)
			.ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper))
			.ConfigureServices(services => services
				.AddSingleton(fakeBrandCertClient)
				.AddSingleton(fakeBrandServiceClient)
				.AddSingleton(fakePlayerServiceClient)
				.AddSingleton(fakePlayerPasswordValidateCommandService)
				.AddSingleton(fakePlayerLoginRecordRepository)
				.AddSingleton(fakeBrandRepository)
				.AddSingleton(fakeValidatePlayerLoginRecordCommandService)
				.AddSingleton(fakePlayerTokenParserCommandService)
				.AddSingleton(fakeMessageSender))
			.Build();

		await web.StartAsync();

		var playerCode = "test1";
		var userName = "test01";
		var password = "password";
		var platformType = PlatformType.Desktop;

		_ = fakeBrandServiceClient.GetBrandStatusAsync(Arg.Is(brandId))
			.Returns((BrandStatus)BrandStatus.Active);

		_ = fakePlayerServiceClient.GetPlayerInfoByUserNameAsync(Arg.Is(brandId), Arg.Is(userName))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				BrandId = brandId,
				UserName = userName,
				PlayerCode = playerCode
			});

		_ = fakePlayerPasswordValidateCommandService.GetAsync(
				Arg.Is<PlayerPasswordValidateCommand>(x =>
					x.PlayerCode == playerCode && x.Password == password))
			.Returns(true);

		var loginRecord = new LoginRecordInfo(
			Guid.NewGuid(),
			playerCode,
			TGS.Core.Models.Enums.LoginStatus.Enabled,
			TGS.Core.Models.Enums.PlatformType.Desktop,
			DateTimeOffset.Now,
			DateTimeOffset.Now.AddDays(1D));

		_ = fakePlayerLoginRecordRepository.AddPlayerLoginRecordAsync(
				Arg.Is(brandId),
				Arg.Is(playerCode),
				Arg.Is(TGS.Core.Models.Enums.PlatformType.Desktop),
				Arg.Any<string>())
			.Returns(loginRecord);

		_ = fakePlayerTokenParserCommandService.GetAsync(
				Arg.Any<PlayerTokenParserCommand>())
			.Returns(new PlayerTokenData
			{
				PlayerCode = playerCode,
				LoginRecordId = loginRecord.Id,
				ExpiredOn = loginRecord.ExpiredOn,
				PlatformType = platformType
			});

		await using var scope = web.Services.CreateAsyncScope();

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, username, password, platformtype) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken);

				let svc = new OperatorService(instance);

				return await svc.playerLogin(username, password, platformtype);
			};
		""";

		// Act
		_ = await StaticNodeJSService.InvokeFromStringAsync<LoginResponse>(
			nodeModule,
			args: new object?[] { brandToken, userName, password, (int)platformType });

		// Assert
		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
				Arg.Is("tgs.operator.player.logged_in"),
				Arg.Any<ReadOnlyMemory<byte>>(),
				Arg.Any<CancellationToken>());
	}
}
