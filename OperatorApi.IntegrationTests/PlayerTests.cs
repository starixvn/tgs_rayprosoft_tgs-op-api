using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Jering.Javascript.NodeJS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Tgs.BrandCertificate.Clients;
using Tgs.Core.Infrastructures.Security;
using Tgs.FileService.Clients;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using Tgs.VipLevelService.Clients;
using TGS.Apis.OperatorApi.Models;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Models.Enums;
using TGS.Services.Operations;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Player;
using TGS.Services.Operator.PlayerRegisterFlow;
using Valhalla.MessageQueue;
using Xunit.Abstractions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using BrandStatus = Tgs.BrandCertificate.BrandStatus;

namespace OperatorApi.IntegrationTests;

public class PlayerTests(ITestOutputHelper testOutputHelper)
{
	private readonly ITestOutputHelper _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));

	[Fact]
	public async Task 玩家身分審核上傳()
	{
		var channelName = Guid.NewGuid().ToString();

		var fakeBrandCertClient = Substitute.For<IBrandCertificateClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakePlayerPasswordValidateCommandService = Substitute.For<IGetService<PlayerPasswordValidateCommand, bool>>();
		var fakePlayerLoginRecordRepository = Substitute.For<IPlayerLoginRecordRepository>();
		var fakeBrandRepository = Substitute.For<IBrandRepository>();
		var fakeValidatePlayerLoginRecordCommandService = Substitute.For<ICommandService<ValidatePlayerLoginRecordCommand>>();
		var fakeFileServiceClient = Substitute.For<IFileServiceClient>();
		var fakePlayerOperations = Substitute.For<IPlayerOperations>();

		_ = fakeBrandCertClient.GetSystemValidateSecurityKeyAsync(Arg.Any<CancellationToken>())
			.Returns(TestOperatorApplication.TestSecurityKey);

		var brandId = Guid.NewGuid();
		var brandCode = "Test";
		var brandToken = TestOperatorApplication.MakeTestBrandToken(brandId, brandCode);
		var secretKey = "N2KAnljo5zbLCZDSoUBFJ70ONsy";

		_ = fakeBrandRepository.GetSecretKeyAsync(
				Arg.Is(brandId),
				Arg.Any<CancellationToken>())
			.Returns(secretKey);

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
				.AddSingleton(fakeFileServiceClient)
				.AddSingleton(fakePlayerOperations))
			.Build();

		await web.StartAsync();

		var playerCode = "test1";
		var userName = "test01";

		_ = fakeBrandServiceClient.GetBrandStatusAsync(
				Arg.Is(brandId),
				Arg.Any<CancellationToken>())
			.Returns(BrandStatus.Active);

		_ = fakePlayerServiceClient.GetPlayerInfoByUserNameAsync(
				Arg.Is(brandId),
				Arg.Is(userName),
				Arg.Any<CancellationToken>())
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				BrandId = brandId,
				UserName = userName,
				PlayerCode = playerCode
			});

		_ = fakePlayerPasswordValidateCommandService.GetAsync(
			Arg.Any<PlayerPasswordValidateCommand>(),
			Arg.Any<CancellationToken>())
			.Returns(true);

		_ = fakePlayerLoginRecordRepository.AddPlayerLoginRecordAsync(
				Arg.Is(brandId),
				Arg.Is(playerCode),
				Arg.Is(PlatformType.Desktop),
				Arg.Any<string>())
			.Returns(new LoginRecordInfo(
			Guid.NewGuid(),
			playerCode,
			LoginStatus.Enabled,
			PlatformType.Desktop,
			DateTimeOffset.Now,
			DateTimeOffset.Now.AddDays(1D)));

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(playerCode), Arg.Any<CancellationToken>())
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				BrandId = brandId,
				UserName = userName,
				PlayerCode = playerCode
			});

		await using var scope = web.Services.CreateAsyncScope();

		var loginService = scope.ServiceProvider.GetRequiredService<IGetService<LoginCommand, LoginInfo>>();

		var loginCommand = new LoginCommand(
			brandId,
			userName,
			"test123",
			"127.0.0.1",
			"localhost",
			TGS.Core.Models.Enums.PlatformType.Desktop,
			"operator-api");

		var loginResult = await loginService.GetAsync(loginCommand);

		var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets/test.jpg");

		_ = fakeFileServiceClient
			.UploadFileAsync(Arg.Any<string>(), Arg.Any<MediaTypeHeaderValue>(), Arg.Any<Stream>())
			.Returns(new FileView()
			{
				Id = "fileId"
			});

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId), Arg.Any<CancellationToken>())
			.Returns(new Tgs.BrandCertificate.Clients.BrandInfo
			{
				Id = brandId,
				PlayerIdentityEnabled = true,
			});

		var request = new
		{
			Id = "newIdentity",
			Realname = "newRealName"
		};

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken, request, filePath) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new OperatorService(instance);

				return await svc.createIdentityAuthenticationForm(request,filePath);
			};
		""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<string>(
			nodeModule,
			args: [brandToken, loginResult.PlayerToken, request, filePath]);

		// Assert
		_ = fakePlayerOperations
			.Received(1)
			.SetIdentityAsync(Arg.Is(playerCode), Arg.Any<string>(), Arg.Any<string>(), Arg.Is(false), Arg.Any<DateTimeOffset>());
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
		var fakeTokenHandler = Substitute.For<IJwtTokenHandler>();

		_ = fakeBrandCertClient.GetSystemValidateSecurityKeyAsync(Arg.Any<CancellationToken>())
			.Returns(TestOperatorApplication.TestSecurityKey);

		var brandId = Guid.NewGuid();
		var brandCode = "Test";
		var brandToken = TestOperatorApplication.MakeTestBrandToken(brandId, brandCode);
		var secretKey = "N2KAnljo5zbLCZDSoUBFJ70ONsy";

		_ = fakeBrandRepository.GetSecretKeyAsync(
				Arg.Is(brandId),
				Arg.Any<CancellationToken>())
			.Returns(secretKey);

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
				.AddSingleton(fakeTokenHandler))
			.Build();

		await web.StartAsync();

		var playerCode = "test1";
		var userName = "test01";
		var password = "password";
		var platformType = PlatformType.Desktop;

		_ = fakeBrandServiceClient.GetBrandStatusAsync(
				Arg.Is(brandId),
				Arg.Any<CancellationToken>())
			.Returns(BrandStatus.Active);

		_ = fakePlayerServiceClient.GetPlayerInfoByUserNameAsync(
				Arg.Is(brandId),
				Arg.Is(userName),
				Arg.Any<CancellationToken>())
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				BrandId = brandId,
				UserName = userName,
				PlayerCode = playerCode
			});

		_ = fakePlayerPasswordValidateCommandService
			.GetAsync(
				Arg.Is<PlayerPasswordValidateCommand>(x =>
					x.PlayerCode == playerCode && x.Password == password),
				Arg.Any<CancellationToken>())
			.Returns(true);

		var loginRecord = new LoginRecordInfo(
			Guid.NewGuid(),
			playerCode,
			LoginStatus.Enabled,
			platformType,
			DateTimeOffset.UtcNow,
			DateTimeOffset.UtcNow.AddDays(1D));

		_ = fakePlayerLoginRecordRepository.AddPlayerLoginRecordAsync(
		Arg.Is(brandId),
				Arg.Is(playerCode),
				Arg.Is(platformType),
				Arg.Any<string>())
			.Returns(loginRecord);

		var identity = new ClaimsIdentity(
			new[]
			{
				new Claim(Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Sub, playerCode),
				new Claim("login_id", loginRecord.Id.ToString()),
				new Claim("platform_type", platformType.ToString())
			});

		var signature = Encoding.UTF8.GetBytes($"{secretKey}{secretKey}");

		var tokenHandler = new JwtSecurityTokenHandler();
		var key = new SymmetricSecurityKey(signature);
		var credential = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

		var descriptor = new SecurityTokenDescriptor
		{
			Subject = identity,
			Issuer = "operator-api",
			Expires = DateTime.UtcNow.AddDays(1D),
			SigningCredentials = credential
		};

		var securityToken = tokenHandler.CreateToken(descriptor);

		var expected = "testToken";

		_ = fakeTokenHandler.GenerateToken(
			Arg.Any<ClaimsIdentity>(),
			Arg.Any<byte[]>(),
			Arg.Is("operator-api"),
			Arg.Any<DateTime?>())
			.Returns(expected);

		_ = fakeTokenHandler.ReadToken(Arg.Is(expected))
			.Returns(securityToken);

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
			args: [brandToken, userName, password, (int)platformType]);

		// Assert
		Assert.NotNull(actual);
		Assert.Equal(expected, actual.PlayerToken);
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

		var fakeMessageSender = Substitute.For<IMessageSender>();

		_ = fakeBrandCertClient.GetSystemValidateSecurityKeyAsync(Arg.Any<CancellationToken>())
			.Returns(TestOperatorApplication.TestSecurityKey);

		var brandId = Guid.NewGuid();
		var brandCode = "Test";
		var brandToken = TestOperatorApplication.MakeTestBrandToken(brandId, brandCode);
		var secretKey = "N2KAnljo5zbLCZDSoUBFJ70ONsy";

		_ = fakeBrandRepository.GetSecretKeyAsync(
				Arg.Is(brandId),
				Arg.Any<CancellationToken>())
			.Returns(secretKey);

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
				.AddSingleton(fakeMessageSender))
			.Build();

		await web.StartAsync();

		var playerCode = "test1";
		var userName = "test01";
		var password = "password";
		var platformType = PlatformType.Desktop;

		_ = fakeBrandServiceClient.GetBrandStatusAsync(
				Arg.Is(brandId),
				Arg.Any<CancellationToken>())
			.Returns(BrandStatus.Active);

		_ = fakePlayerServiceClient.GetPlayerInfoByUserNameAsync(
				Arg.Is(brandId),
				Arg.Is(userName),
				Arg.Any<CancellationToken>())
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				BrandId = brandId,
				UserName = userName,
				PlayerCode = playerCode
			});

		_ = fakePlayerPasswordValidateCommandService.GetAsync(
				Arg.Is<PlayerPasswordValidateCommand>(x =>
					x.PlayerCode == playerCode && x.Password == password),
				Arg.Any<CancellationToken>())
			.Returns(true);

		var loginRecord = new LoginRecordInfo(
			Guid.NewGuid(),
			playerCode,
			LoginStatus.Enabled,
			PlatformType.Desktop,
			DateTimeOffset.Now,
			DateTimeOffset.Now.AddDays(1D));

		_ = fakePlayerLoginRecordRepository.AddPlayerLoginRecordAsync(
				Arg.Is(brandId),
				Arg.Is(playerCode),
				Arg.Is(PlatformType.Desktop),
				Arg.Any<string>())
			.Returns(loginRecord);

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
			args: [brandToken, userName, password, (int)platformType]);

		// Assert
		_ = fakeMessageSender
			.Received(1)
			.PublishAsync(
				Arg.Is("tgs.operator.player.logged_in"),
				Arg.Any<byte[]>(),
				Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task 註冊玩家()
	{
		var channelName = Guid.NewGuid().ToString();

		var fakeBrandCertClient = Substitute.For<IBrandCertificateClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandRepository = Substitute.For<IBrandRepository>();
		var fakeVipServiceClient = Substitute.For<IVipLevelServiceClient>();

		_ = fakeBrandCertClient.GetSystemValidateSecurityKeyAsync(Arg.Any<CancellationToken>())
			.Returns(TestOperatorApplication.TestSecurityKey);

		var brandId = Guid.NewGuid();
		var brandCode = "Test";
		var brandToken = TestOperatorApplication.MakeTestBrandToken(brandId, brandCode);
		var secretKey = "N2KAnljo5zbLCZDSoUBFJ70ONsy";

		_ = fakeBrandRepository.GetSecretKeyAsync(
				Arg.Is(brandId),
				Arg.Any<CancellationToken>())
			.Returns(secretKey);

		using var web = TestOperatorApplication.Builder
			.UseTestChannelServer(channelName)
			.ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper))
			.ConfigureServices(services => services
				.AddSingleton(fakeBrandCertClient)
				.AddSingleton(fakeBrandServiceClient)
				.AddSingleton(fakePlayerServiceClient)
				.AddSingleton(fakeBrandRepository)
				.AddSingleton(fakeVipServiceClient))
			.Build();

		await web.StartAsync();

		var stepName = "CreatePlayer";
		var userNamePasswordInput = new CreatePlayerRegisterFlowStep.CreatePlayerInputData
		{
			UserName = "test1",
			Password = "password"
		};

		var request = new PlayerRegisterFlowViewModel
		{
			Type = stepName,
			Input = JsonSerializer.SerializeToDocument(userNamePasswordInput)
		};

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId), Arg.Any<CancellationToken>())
			.Returns(new Tgs.BrandCertificate.Clients.BrandInfo
			{
				Id = brandId,
				Code = brandCode,
				Currency = "CNY",
				BankInfoRegisterFlowEnabled = false
			});

		_ = fakeBrandServiceClient.GetBrandFrontendInfoAsync(Arg.Is(brandId), Arg.Any<CancellationToken>())
			.Returns(new BrandFrontendInfo(
				null,
				null,
				null,
				null));

		_ = fakeBrandServiceClient.GetBrandBetLimitIdAsync(Arg.Is(brandId), Arg.Any<CancellationToken>())
			.Returns(100);

		var vipLevelInfo = new Tgs.VipLevelService.Common.ViewModels.VipLevelInfoViewModel(
				Id: Guid.NewGuid(),
				Level: 1,
				BrandId: brandId,
				LevelUpAccumulateBetAmount: 0M,
				LevelUpAccumulateDeposit: 0M,
				LevelKeepAccumulateBetAmount: 0M,
				LevelKeepAccumulateDeposit: 0M,
				LevelKeepCondition: false,
				BirthBenefit: 0M,
				PromotionBenefit: 0M,
				QuarterlyBenefit: 0M,
				LiveDealerRebate: 0M,
				SlotRebate: 0M,
				LotteryRebate: 0M,
				CardGameRebate: 0M,
				SportsRebate: 0M,
				VirtualSportsRebate: 0M,
				WithdrawTransactionsPerDay: 0,
				WithdrawTotalAmountPerDay: 0M);
		_ = fakeVipServiceClient.VipLevelGetAsync(
			brandId: Arg.Is(brandId),
			level: Arg.Is(1),
			Arg.Any<CancellationToken>())
			.Returns(vipLevelInfo);

		var resultPlayerCode = "TestPlayerCode";
		_ = fakePlayerServiceClient.CreatePlayerAsync(
			Arg.Is<CreatePlayerData>(data => data.BrandId == brandId && data.Username == userNamePasswordInput.UserName))
			.Returns(resultPlayerCode);

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, stepRequest) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken);

				let svc = new OperatorService(instance);

				return await svc.registerPlayer(stepRequest);
			};
		""";

		// Act
		var response = await StaticNodeJSService.InvokeFromStringAsync<string>(
			nodeModule,
			args: [brandToken, request]);

		var actual = JsonSerializer.Deserialize<SuccessResponse<PlayerRegisterFlowResponseViewModel>>(
			response!,
			web.Services.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions);

		// Assert
		Assert.True(actual?.Data.IsCompleted);
	}

	[Fact]
	public async Task 設定玩家通訊軟體Zalo()
	{
		var channelName = Guid.NewGuid().ToString();

		var fakeBrandCertClient = Substitute.For<IBrandCertificateClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakePlayerPasswordValidateCommandService = Substitute.For<IGetService<PlayerPasswordValidateCommand, bool>>();
		var fakePlayerLoginRecordRepository = Substitute.For<IPlayerLoginRecordRepository>();
		var fakeBrandRepository = Substitute.For<IBrandRepository>();
		var fakeValidatePlayerLoginRecordCommandService = Substitute.For<ICommandService<ValidatePlayerLoginRecordCommand>>();

		_ = fakeBrandCertClient.GetSystemValidateSecurityKeyAsync(Arg.Any<CancellationToken>())
			.Returns(TestOperatorApplication.TestSecurityKey);

		var brandId = Guid.NewGuid();
		var brandCode = "Test";
		var brandToken = TestOperatorApplication.MakeTestBrandToken(brandId, brandCode);
		var secretKey = "N2KAnljo5zbLCZDSoUBFJ70ONsy";

		_ = fakeBrandRepository.GetSecretKeyAsync(
				Arg.Is(brandId),
				Arg.Any<CancellationToken>())
			.Returns(secretKey);

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
				.AddSingleton(fakeValidatePlayerLoginRecordCommandService))
			.Build();

		await web.StartAsync();

		var playerCode = "test1";
		var userName = "test01";

		_ = fakeBrandServiceClient.GetBrandStatusAsync(
				Arg.Is(brandId),
				Arg.Any<CancellationToken>())
			.Returns(BrandStatus.Active);

		_ = fakePlayerServiceClient.GetPlayerInfoByUserNameAsync(
				Arg.Is(brandId),
				Arg.Is(userName),
				Arg.Any<CancellationToken>())
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				BrandId = brandId,
				UserName = userName,
				PlayerCode = playerCode
			});

		_ = fakePlayerPasswordValidateCommandService.GetAsync(
			Arg.Any<PlayerPasswordValidateCommand>(),
			Arg.Any<CancellationToken>())
			.Returns(true);

		_ = fakePlayerLoginRecordRepository.AddPlayerLoginRecordAsync(
				Arg.Is(brandId),
				Arg.Is(playerCode),
				Arg.Is(PlatformType.Desktop),
				Arg.Any<string>())
			.Returns(new LoginRecordInfo(
			Guid.NewGuid(),
			playerCode,
			LoginStatus.Enabled,
			PlatformType.Desktop,
			DateTimeOffset.Now,
			DateTimeOffset.Now.AddDays(1D)));

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(playerCode), Arg.Any<CancellationToken>())
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				BrandId = brandId,
				UserName = userName,
				PlayerCode = playerCode
			});

		await using var scope = web.Services.CreateAsyncScope();

		var loginService = scope.ServiceProvider.GetRequiredService<IGetService<LoginCommand, LoginInfo>>();

		var loginCommand = new LoginCommand(
			brandId,
			userName,
			"test123",
			"127.0.0.1",
			"localhost",
			TGS.Core.Models.Enums.PlatformType.Desktop,
			"operator-api");

		var loginResult = await loginService.GetAsync(loginCommand);

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId), Arg.Any<CancellationToken>())
			.Returns(new Tgs.BrandCertificate.Clients.BrandInfo
			{
				Id = brandId,
				PlayerIdentityEnabled = true,
			});

		var request = new SetZaloRequest
		{
			Zalo = "testZaloInfo"
		};

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken, request) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new OperatorService(instance);

				return await svc.setPlayerZalo(request);
			};
		""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<string>(
			nodeModule,
			args: [brandToken, loginResult.PlayerToken, request]);

		// Assert
		_ = fakePlayerServiceClient
			.Received(1)
			.SetPlayerPropertiesAsync(
				Arg.Is(playerCode),
				Arg.Is<IImmutableDictionary<PlayerPropertyName, string?>>(
					dict => dict[PlayerPropertyName.Zalo] == "testZaloInfo"),
				Arg.Any<string?>(),
				Arg.Any<CancellationToken>());
	}
}
