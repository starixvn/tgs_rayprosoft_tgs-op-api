using System.Runtime.InteropServices;
using System.Text.Json;
using Jering.Javascript.NodeJS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tgs.BrandCertificate;
using Tgs.BrandCertificate.Clients;
using Tgs.Payment.Clients;
using Tgs.Payment.FiveSevenPay.Client;
using Tgs.Payment.MetaPay.Client;
using Tgs.Payment.PhpGoPay.Client;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Models;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Payment.ManualDeposit;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Deposit;
using TGS.Services.Operator.ApplicationServices.Player;
using Xunit.Abstractions;

namespace OperatorApi.IntegrationTests;

public class DepositTests
{
	private readonly ITestOutputHelper _testOutputHelper;

	public DepositTests(ITestOutputHelper testOutputHelper)
	{
		_testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
	}

	[Fact]
	public async Task FiveSevenPay存款()
	{
		// Arrange
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

		_ = fakeBrandRepository.GetSecretKeyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
			.Returns("N2KAnljo5zbLCZDSoUBFJ70ONsy");

		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakePromotionClient = Substitute.For<IPromotionClient>();

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
				.AddSingleton(fakePaymentServiceClient)
				.AddSingleton(fakePromotionClient))
			.Build();

		await web.StartAsync();

		var playerCode = "test1";
		var userName = "test";

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

		_ = fakePlayerPasswordValidateCommandService.GetAsync(
			Arg.Any<PlayerPasswordValidateCommand>(),
			Arg.Any<CancellationToken>())
			.Returns(true);

		_ = fakePlayerLoginRecordRepository.AddPlayerLoginRecordAsync(
			Arg.Is(brandId),
			Arg.Is(playerCode),
			Arg.Is(TGS.Core.Models.Enums.PlatformType.Desktop),
			Arg.Is("127.0.0.1"))
			.Returns(new LoginRecordInfo(
				Guid.NewGuid(),
				playerCode,
				TGS.Core.Models.Enums.LoginStatus.Enabled,
				TGS.Core.Models.Enums.PlatformType.Desktop,
				DateTimeOffset.Now,
				DateTimeOffset.Now.AddDays(1D)));

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

		var request = new SubmitDepositRequest
		{
			Amount = 100M,
			BrandPaymentId = Guid.NewGuid(),
			PromotionId = null
		};

		var brandPaymentInfo = new BrandPaymentInfo
		{
			Id = request.BrandPaymentId,
			ChannelId = Guid.NewGuid(),
			ChannelName = "Test",
			Enabled = true,
			ExtendedData = """
				{
				  "payType": "Gcash",
				  "currency": "PHP",
				  "publicKey": "123456",
				  "privateKey": "98765",
				  "merchantKey": "test123"
				}
				""",
			PaymentId = Guid.NewGuid(),
			BrandId = brandId
		};

		_ = fakePaymentServiceClient.GetBrandPaymentByIdAsync(
			Arg.Is(request.BrandPaymentId),
			Arg.Any<CancellationToken>())
			.Returns(brandPaymentInfo);

		_ = fakePaymentServiceClient.GetPaymentByIdAsync(
			Arg.Is(brandPaymentInfo.PaymentId),
			Arg.Any<CancellationToken>())
			.Returns(new PaymentInfo
			{
				Id = brandPaymentInfo.PaymentId,
				Code = "FiveSevenPay"
			});

		_ = fakePaymentServiceClient.LockDepositPaymentAsync(
			Arg.Any<Guid>(),
			Arg.Is("FiveSevenPay"),
			Arg.Is(playerCode),
			Arg.Any<CancellationToken>())
			.Returns(callInfo => callInfo.ArgAt<Guid>(0));

		var formId = Guid.NewGuid();

		var applyInfo = new FiveSevenPayDepositApplyInfo(
			new FiveSevenPayDepositSettings(
				"test123",
				"98765",
				"123456",
				"PHP",
				FiveSevenPayType.GCash,
				formId),
			new FiveSevenPayDepositResponse(
				formId,
				100M,
				null,
				"",
				new Uri("http://localhost")));

		_ = fakePaymentServiceClient.GetDepositFormAsync(
			Arg.Is(brandId),
			Arg.Any<Guid>(),
			Arg.Any<CancellationToken>())
			.Returns(new DepositFormInfo
			{
				PlayerCode = playerCode,
				ApplyInfo = JsonSerializer.Serialize(applyInfo, FiveSevenPayUtility.SerializerOptions)
			});

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken, request) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new OperatorService(instance);

				return await svc.submitFiveSevenThirdPartyDepositRequest(request);
			};
			""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<SuccessResponse<DepositFormCreatedResponse>>(
			nodeModule,
			args: new object?[] { brandToken, loginResult.PlayerToken, request });

		// Assert
		Assert.NotNull(actual);
		Assert.Equal(applyInfo.Response.RedirectUrl.ToString(), actual.Data.PaymentUrl);
	}

	[Fact]
	public async Task PhpGo存款()
	{
		// Arrange
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

		_ = fakeBrandRepository.GetSecretKeyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
			.Returns("N2KAnljo5zbLCZDSoUBFJ70ONsy");

		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakePromotionClient = Substitute.For<IPromotionClient>();

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
				.AddSingleton(fakePaymentServiceClient)
				.AddSingleton(fakePromotionClient))
			.Build();

		await web.StartAsync();

		var playerCode = "test1";
		var userName = "test";

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
		_ = fakePlayerPasswordValidateCommandService.GetAsync(
			Arg.Any<PlayerPasswordValidateCommand>(),
			Arg.Any<CancellationToken>())
			.Returns(true);
		_ = fakePlayerLoginRecordRepository.AddPlayerLoginRecordAsync(
			Arg.Is(brandId),
			Arg.Is(playerCode),
			Arg.Is(TGS.Core.Models.Enums.PlatformType.Desktop),
			Arg.Is("127.0.0.1"))
			.Returns(new LoginRecordInfo(
				Guid.NewGuid(),
				playerCode,
				TGS.Core.Models.Enums.LoginStatus.Enabled,
				TGS.Core.Models.Enums.PlatformType.Desktop,
				DateTimeOffset.Now,
				DateTimeOffset.Now.AddDays(1D)));

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

		var request = new SubmitDepositRequest
		{
			Amount = 100M,
			BrandPaymentId = Guid.NewGuid(),
			PromotionId = null
		};

		var brandPaymentInfo = new BrandPaymentInfo
		{
			Id = request.BrandPaymentId,
			ChannelId = Guid.NewGuid(),
			ChannelName = "Test",
			Enabled = true,
			ExtendedData = """
				{
					"merchant": "test123",
					"key": "aabbccddeeff",
					"paymentType": "3",
					"bankCode": "gcash"
				}
				""",
			PaymentId = Guid.NewGuid(),
			BrandId = brandId
		};

		_ = fakePaymentServiceClient.GetBrandPaymentByIdAsync(
			Arg.Is(request.BrandPaymentId),
			Arg.Any<CancellationToken>())
			.Returns(brandPaymentInfo);

		_ = fakePaymentServiceClient.GetPaymentByIdAsync(
			Arg.Is(brandPaymentInfo.PaymentId),
			Arg.Any<CancellationToken>())
			.Returns(new PaymentInfo
			{
				Id = brandPaymentInfo.PaymentId,
				Code = "PhpGoPay"
			});

		_ = fakePaymentServiceClient.LockDepositPaymentAsync(
			Arg.Any<Guid>(),
			Arg.Is("PhpGoPay"),
			Arg.Is(playerCode),
			Arg.Any<CancellationToken>())
			.Returns(callInfo => callInfo.ArgAt<Guid>(0));

		var applyInfo = new PhpGoPayDepositApplyInfo(
			new PhpGoPayDepositSettings(
				"test123",
				"aabbccddeeff",
				PhpGoPaymentType.Direct,
				PhpGoBankCode.GCash,
				new Uri("http://localhost"),
				Guid.NewGuid()),
			new PhpGoPayDepositResponse(
				100M,
				null,
				Guid.NewGuid(),
				new Uri("http://localhost/test"),
				new Uri("http://localhost")));

		_ = fakePaymentServiceClient.GetDepositFormAsync(
			Arg.Is(brandId),
			Arg.Any<Guid>(),
			Arg.Any<CancellationToken>())
			.Returns(new DepositFormInfo
			{
				PlayerCode = playerCode,
				ApplyInfo = JsonSerializer.Serialize(applyInfo, PhpGoPayUtility.SerializerOptions)
			});

		var expected = new DepositFormCreatedResponse
		{
			FormId = Guid.NewGuid(),
			PaymentUrl = "https://localhost"
		};

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken, request) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new OperatorService(instance);

				return await svc.submitPhpGoThirdPartyDepositRequest(request);
			};
			""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<SuccessResponse<DepositFormCreatedResponse>>(
			nodeModule,
			args: new object?[] { brandToken, loginResult.PlayerToken, request });

		// Assert
		Assert.Equal(applyInfo.Response.RedirectUrl.ToString(), actual?.Data.PaymentUrl);
	}

	[Fact]
	public async Task MetaPay三方存款()
	{
		// Arrange
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

		_ = fakeBrandRepository.GetSecretKeyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
			.Returns("N2KAnljo5zbLCZDSoUBFJ70ONsy");

		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakePromotionClient = Substitute.For<IPromotionClient>();

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
				.AddSingleton(fakePaymentServiceClient)
				.AddSingleton(fakePromotionClient))
			.Build();

		await web.StartAsync();

		var playerCode = "test1";
		var userName = "test";

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
		_ = fakePlayerPasswordValidateCommandService.GetAsync(
			Arg.Any<PlayerPasswordValidateCommand>(),
			Arg.Any<CancellationToken>())
			.Returns(true);
		_ = fakePlayerLoginRecordRepository.AddPlayerLoginRecordAsync(
			Arg.Is(brandId),
			Arg.Is(playerCode),
			Arg.Is(TGS.Core.Models.Enums.PlatformType.Desktop),
			Arg.Is("127.0.0.1"))
			.Returns(new LoginRecordInfo(
				Guid.NewGuid(),
				playerCode,
				TGS.Core.Models.Enums.LoginStatus.Enabled,
				TGS.Core.Models.Enums.PlatformType.Desktop,
				DateTimeOffset.Now,
				DateTimeOffset.Now.AddDays(1D)));

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

		var request = new SubmitDepositRequest
		{
			Amount = 100M,
			BrandPaymentId = Guid.NewGuid(),
			PromotionId = null
		};

		var brandPaymentInfo = new BrandPaymentInfo
		{
			Id = request.BrandPaymentId,
			ChannelId = Guid.NewGuid(),
			ChannelName = "Test",
			Enabled = true,
			ExtendedData = """
				{
					"appId": "appId",
					"privateKey": "privateKey",
					"publicKey": "publicKey",					
					"payType": "UB_ONLINE"
				}
				""",
			PaymentId = Guid.NewGuid(),
			BrandId = brandId
		};

		_ = fakePaymentServiceClient.GetBrandPaymentByIdAsync(
			Arg.Is(request.BrandPaymentId),
			Arg.Any<CancellationToken>())
			.Returns(brandPaymentInfo);

		_ = fakePaymentServiceClient.GetPaymentByIdAsync(
			Arg.Is(brandPaymentInfo.PaymentId),
			Arg.Any<CancellationToken>())
			.Returns(new PaymentInfo
			{
				Id = brandPaymentInfo.PaymentId,
				Code = "MetaPay"
			});

		_ = fakePaymentServiceClient.LockDepositPaymentAsync(
			Arg.Any<Guid>(),
			Arg.Is("MetaPay"),
			Arg.Is(playerCode),
			Arg.Any<CancellationToken>())
			.Returns(callInfo => callInfo.ArgAt<Guid>(0));

		var applyInfo = new MetaPayDepositApplyInfo(
			new MetaPayDepositSettings(
				"appId",
				"privateKey",
				"publicKey",
				MetaPayDepositType.Cashier_Desk,				
				new Uri("http://localhost"),
				Guid.NewGuid(),
				"",
				""),
			new MetaPayDepositResponse(
				100M,
				null,
				Guid.NewGuid(),
				new Uri("http://localhost/test")));

		_ = fakePaymentServiceClient.GetDepositFormAsync(
			Arg.Is(brandId),
			Arg.Any<Guid>(),
			Arg.Any<CancellationToken>())
			.Returns(new DepositFormInfo
			{
				PlayerCode = playerCode,
				ApplyInfo = JsonSerializer.Serialize(applyInfo, MetaPayUtility.SerializerOptions)
			});

		var expected = new DepositFormCreatedResponse
		{
			FormId = Guid.NewGuid(),
			PaymentUrl = "https://localhost"
		};

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken, request) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new OperatorService(instance);

				return await svc.submitMetaThirdPartyDepositRequest(request);
			};
			""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<SuccessResponse<DepositFormCreatedResponse>>(
			nodeModule,
			args: new object?[] { brandToken, loginResult.PlayerToken, request });

		// Assert
		Assert.Equal(applyInfo.Response.RedirectUrl.ToString(), actual?.Data.PaymentUrl);
	}

	[Fact]
	public async Task MetaPay銀行存款()
	{
		// Arrange
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

		_ = fakeBrandRepository.GetSecretKeyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
			.Returns("N2KAnljo5zbLCZDSoUBFJ70ONsy");

		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakePromotionClient = Substitute.For<IPromotionClient>();

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
				.AddSingleton(fakePaymentServiceClient)
				.AddSingleton(fakePromotionClient))
			.Build();

		await web.StartAsync();

		var playerCode = "test1";
		var userName = "test";

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
		_ = fakePlayerPasswordValidateCommandService.GetAsync(
			Arg.Any<PlayerPasswordValidateCommand>(),
			Arg.Any<CancellationToken>())
			.Returns(true);
		_ = fakePlayerLoginRecordRepository.AddPlayerLoginRecordAsync(
			Arg.Is(brandId),
			Arg.Is(playerCode),
			Arg.Is(TGS.Core.Models.Enums.PlatformType.Desktop),
			Arg.Is("127.0.0.1"))
			.Returns(new LoginRecordInfo(
				Guid.NewGuid(),
				playerCode,
				TGS.Core.Models.Enums.LoginStatus.Enabled,
				TGS.Core.Models.Enums.PlatformType.Desktop,
				DateTimeOffset.Now,
				DateTimeOffset.Now.AddDays(1D)));

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

		var request = new SubmitDepositRequest
		{
			Amount = 100M,
			BrandPaymentId = Guid.NewGuid(),
			PromotionId = null
		};

		var brandPaymentInfo = new BrandPaymentInfo
		{
			Id = request.BrandPaymentId,
			ChannelId = Guid.NewGuid(),
			ChannelName = "Test",
			Enabled = true,
			ExtendedData = """
				{
					"appId": "appId",
					"privateKey": "privateKey",
					"publicKey": "publicKey",					
					"payType": "PAYMAYA"
				}
				""",
			PaymentId = Guid.NewGuid(),
			BrandId = brandId
		};

		_ = fakePaymentServiceClient.GetBrandPaymentByIdAsync(
			Arg.Is(request.BrandPaymentId),
			Arg.Any<CancellationToken>())
			.Returns(brandPaymentInfo);

		_ = fakePaymentServiceClient.GetPaymentByIdAsync(
			Arg.Is(brandPaymentInfo.PaymentId),
			Arg.Any<CancellationToken>())
			.Returns(new PaymentInfo
			{
				Id = brandPaymentInfo.PaymentId,
				Code = "MetaPay"
			});

		_ = fakePaymentServiceClient.LockDepositPaymentAsync(
			Arg.Any<Guid>(),
			Arg.Is("MetaPay"),
			Arg.Is(playerCode),
			Arg.Any<CancellationToken>())
			.Returns(callInfo => callInfo.ArgAt<Guid>(0));

		var applyInfo = new MetaPayDepositApplyInfo(
			new MetaPayDepositSettings(
				"appId",
				"privateKey",
				"publicKey",
				MetaPayDepositType.Cashier_Desk,
				new Uri("http://localhost"),
				Guid.NewGuid(),
				"",
				""),
			new MetaPayDepositResponse(
				100M,
				null,
				Guid.NewGuid(),
				new Uri("http://localhost/test")));

		_ = fakePaymentServiceClient.GetDepositFormAsync(
			Arg.Is(brandId),
			Arg.Any<Guid>(),
			Arg.Any<CancellationToken>())
			.Returns(new DepositFormInfo
			{
				PlayerCode = playerCode,
				ApplyInfo = JsonSerializer.Serialize(applyInfo, MetaPayUtility.SerializerOptions)
			});

		var expected = new DepositFormCreatedResponse
		{
			FormId = Guid.NewGuid(),
			PaymentUrl = "https://localhost"
		};

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken, request) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new OperatorService(instance);

				return await svc.submitMetaBankDepositRequest(request);
			};
			""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<SuccessResponse<DepositFormCreatedResponse>>(
			nodeModule,
			args: new object?[] { brandToken, loginResult.PlayerToken, request });

		// Assert
		Assert.Equal(applyInfo.Response.RedirectUrl.ToString(), actual?.Data.PaymentUrl);
	}

	[Fact]
	public async Task 手動USDT存款時QRCode為非必填()
	{
		// Arrange
		var channelName = Guid.NewGuid().ToString();

		var fakeBrandCertClient = Substitute.For<IBrandCertificateClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakePlayerPasswordValidateCommandService = Substitute.For<IGetService<PlayerPasswordValidateCommand, bool>>();
		var fakePlayerLoginRecordRepository = Substitute.For<IPlayerLoginRecordRepository>();
		var fakeBrandRepository = Substitute.For<IBrandRepository>();
		var fakeValidatePlayerLoginRecordCommandService = Substitute.For<ICommandService<ValidatePlayerLoginRecordCommand>>();

		var fakeCreateDepositFormService = Substitute.For<IGetService<CreateDepositFormCommand, Guid>>();
		var fakeGetBrandPaymentService = Substitute.For<IGetService<GetBrandPaymentQuery, BrandPaymentInfo>>();

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
				.AddSingleton(fakeCreateDepositFormService)
				.AddSingleton(fakeGetBrandPaymentService)
				.AddSingleton<ManualDepositSerializer>())
			.Build();

		await web.StartAsync();

		var playerCode = "test1";
		var userName = "test";

		_ = fakeBrandServiceClient.GetBrandStatusAsync(Arg.Is(brandId))
			.Returns(BrandStatus.Active);
		_ = fakePlayerServiceClient.GetPlayerInfoByUserNameAsync(Arg.Is(brandId), Arg.Is(userName))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				BrandId = brandId,
				UserName = userName,
				PlayerCode = playerCode
			});
		_ = fakePlayerPasswordValidateCommandService.GetAsync(Arg.Any<PlayerPasswordValidateCommand>())
			.Returns(true);
		_ = fakePlayerLoginRecordRepository.AddPlayerLoginRecordAsync(
			Arg.Is(brandId),
			Arg.Is(playerCode),
			Arg.Is(TGS.Core.Models.Enums.PlatformType.Desktop),
			Arg.Is("127.0.0.1"))
			.Returns(new LoginRecordInfo(
				Guid.NewGuid(),
				playerCode,
				TGS.Core.Models.Enums.LoginStatus.Enabled,
				TGS.Core.Models.Enums.PlatformType.Desktop,
				DateTimeOffset.Now,
				DateTimeOffset.Now.AddDays(1D)));

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

		var brandPaymentId = Guid.NewGuid();
		var amount = 1000m;

		var brandPaymentInfo = new BrandPaymentInfo
		{
			Id = brandPaymentId,
			ChannelId = Guid.NewGuid(),
			ChannelName = "Usdt",
			Enabled = true,
			ExtendedData = """
				{
					"rate": 0.01,
					"address": "usdt_address"
				}
				"""
		};

		_ = fakeGetBrandPaymentService.GetAsync(
			Arg.Is<GetBrandPaymentQuery>(x => x.Id == brandPaymentId),
			Arg.Any<CancellationToken>())
			.Returns(brandPaymentInfo);

		var expected = Guid.NewGuid();

		_ = fakeCreateDepositFormService.GetAsync(
			Arg.Is<CreateDepositFormCommand>(x =>
					x.PlayerCode == playerCode
					&& x.BrandId == brandId
					&& x.BrandPaymentId == brandPaymentId),
			Arg.Any<CancellationToken>())
			.Returns(expected);

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken, amount, paymentId, promotionId, bankType) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new OperatorService(instance);

				return await svc.submitManualDepositUsdt(amount, paymentId, promotionId, bankType);
			};
		""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<SuccessResponse<string>>(
			nodeModule,
			args: new object?[] { brandToken, loginResult.PlayerToken, amount, brandPaymentId, null, "usdt" });

		// Assert
		Assert.Equal(expected, Guid.Parse(actual!.Data));
	}

	[Fact]
	public async Task 手動存款Bank()
	{
		// Arrange
		var channelName = Guid.NewGuid().ToString();

		var fakeBrandCertClient = Substitute.For<IBrandCertificateClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakePlayerPasswordValidateCommandService = Substitute.For<IGetService<PlayerPasswordValidateCommand, bool>>();
		var fakePlayerLoginRecordRepository = Substitute.For<IPlayerLoginRecordRepository>();
		var fakeBrandRepository = Substitute.For<IBrandRepository>();
		var fakeValidatePlayerLoginRecordCommandService = Substitute.For<ICommandService<ValidatePlayerLoginRecordCommand>>();

		var fakeCreateDepositFormService = Substitute.For<IGetService<CreateDepositFormCommand, Guid>>();
		var fakeGetBrandPaymentService = Substitute.For<IGetService<GetBrandPaymentQuery, BrandPaymentInfo>>();

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
				.AddSingleton(fakeCreateDepositFormService)
				.AddSingleton(fakeGetBrandPaymentService)
				.AddSingleton<ManualDepositSerializer>())
			.Build();

		await web.StartAsync();

		var playerCode = "test1";
		var userName = "test";

		_ = fakeBrandServiceClient.GetBrandStatusAsync(Arg.Is(brandId))
			.Returns(BrandStatus.Active);
		_ = fakePlayerServiceClient.GetPlayerInfoByUserNameAsync(Arg.Is(brandId), Arg.Is(userName))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				BrandId = brandId,
				UserName = userName,
				PlayerCode = playerCode
			});
		_ = fakePlayerPasswordValidateCommandService.GetAsync(Arg.Any<PlayerPasswordValidateCommand>())
			.Returns(true);
		_ = fakePlayerLoginRecordRepository.AddPlayerLoginRecordAsync(
				Arg.Is(brandId),
				Arg.Is(playerCode),
				Arg.Is(TGS.Core.Models.Enums.PlatformType.Desktop),
				Arg.Is("127.0.0.1"))
			.Returns(new LoginRecordInfo(
				Guid.NewGuid(),
				playerCode,
				TGS.Core.Models.Enums.LoginStatus.Enabled,
				TGS.Core.Models.Enums.PlatformType.Desktop,
				DateTimeOffset.Now,
				DateTimeOffset.Now.AddDays(1D)));

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

		var brandPaymentId = Guid.NewGuid();
		var amount = 1000m;

		var brandPaymentInfo = new BrandPaymentInfo
		{
			Id = brandPaymentId,
			ChannelId = Guid.NewGuid(),
			ChannelName = "Usdt",
			Enabled = true,
			ExtendedData = """
				{
					"name": "testName",
					"bankAccount": "testBankAccount"
				}
				"""
		};

		_ = fakeGetBrandPaymentService.GetAsync(
			Arg.Is<GetBrandPaymentQuery>(x => x.Id == brandPaymentId),
			Arg.Any<CancellationToken>())
			.Returns(brandPaymentInfo);

		var expected = Guid.NewGuid();

		_ = fakeCreateDepositFormService.GetAsync(
			Arg.Is<CreateDepositFormCommand>(x =>
					x.PlayerCode == playerCode
					&& x.BrandId == brandId
					&& x.BrandPaymentId == brandPaymentId),
			Arg.Any<CancellationToken>())
			.Returns(expected);

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken, amount, paymentId, promotionId, bankType, account, name, note, realname) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new OperatorService(instance);

				return await svc.submitManualDepositBankRequest(amount, paymentId, promotionId, bankType, account, name, note, realname);
			};
			""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<SuccessResponse<string>>(
			nodeModule,
			args: new object?[] { brandToken, loginResult.PlayerToken, amount, brandPaymentId, null, "bank", "testAccount", "testBankName", "testNote", null });

		// Assert
		Assert.Equal(expected, Guid.Parse(actual!.Data));
	}

	[Fact]
	public async Task 手動存款EWallet()
	{
		// Arrange
		var channelName = Guid.NewGuid().ToString();

		var fakeBrandCertClient = Substitute.For<IBrandCertificateClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakePlayerPasswordValidateCommandService = Substitute.For<IGetService<PlayerPasswordValidateCommand, bool>>();
		var fakePlayerLoginRecordRepository = Substitute.For<IPlayerLoginRecordRepository>();
		var fakeBrandRepository = Substitute.For<IBrandRepository>();
		var fakeValidatePlayerLoginRecordCommandService = Substitute.For<ICommandService<ValidatePlayerLoginRecordCommand>>();

		var fakeCreateDepositFormService = Substitute.For<IGetService<CreateDepositFormCommand, Guid>>();
		var fakeGetBrandPaymentService = Substitute.For<IGetService<GetBrandPaymentQuery, BrandPaymentInfo>>();

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
				.AddSingleton(fakeCreateDepositFormService)
				.AddSingleton(fakeGetBrandPaymentService)
				.AddSingleton<ManualDepositSerializer>())
			.Build();

		await web.StartAsync();

		var playerCode = "test1";
		var userName = "test";

		_ = fakeBrandServiceClient.GetBrandStatusAsync(Arg.Is(brandId))
			.Returns(BrandStatus.Active);
		_ = fakePlayerServiceClient.GetPlayerInfoByUserNameAsync(Arg.Is(brandId), Arg.Is(userName))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				BrandId = brandId,
				UserName = userName,
				PlayerCode = playerCode
			});
		_ = fakePlayerPasswordValidateCommandService.GetAsync(Arg.Any<PlayerPasswordValidateCommand>())
			.Returns(true);
		_ = fakePlayerLoginRecordRepository.AddPlayerLoginRecordAsync(
				Arg.Is(brandId),
				Arg.Is(playerCode),
				Arg.Is(TGS.Core.Models.Enums.PlatformType.Desktop),
				Arg.Is("127.0.0.1"))
			.Returns(new LoginRecordInfo(
				Guid.NewGuid(),
				playerCode,
				TGS.Core.Models.Enums.LoginStatus.Enabled,
				TGS.Core.Models.Enums.PlatformType.Desktop,
				DateTimeOffset.Now,
				DateTimeOffset.Now.AddDays(1D)));

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

		var brandPaymentId = Guid.NewGuid();
		var amount = 1000m;

		var brandPaymentInfo = new BrandPaymentInfo
		{
			Id = brandPaymentId,
			ChannelId = Guid.NewGuid(),
			ChannelName = "Usdt",
			Enabled = true,
			ExtendedData = """
				{
					"phone": "0912345678",
					"eMoneyName": "testEMoneyName"
				}
				"""
		};

		_ = fakeGetBrandPaymentService.GetAsync(
			Arg.Is<GetBrandPaymentQuery>(x => x.Id == brandPaymentId),
			Arg.Any<CancellationToken>())
			.Returns(brandPaymentInfo);

		var expected = Guid.NewGuid();

		_ = fakeCreateDepositFormService.GetAsync(
			Arg.Is<CreateDepositFormCommand>(x =>
					x.PlayerCode == playerCode
					&& x.BrandId == brandId
					&& x.BrandPaymentId == brandPaymentId),
			Arg.Any<CancellationToken>())
			.Returns(expected);

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken, amount, paymentId, promotionId, bankType, note, realname, phone) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new OperatorService(instance);

				return await svc.submitManualDepositEWalletRequest(amount, paymentId, promotionId, bankType, note, realname, phone);
			};
		""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<SuccessResponse<string>>(
			nodeModule,
			args: new object?[] { brandToken, loginResult.PlayerToken, amount, brandPaymentId, null, "ewallet", "testNote", "testRealName", "0912345678" });

		// Assert
		Assert.Equal(expected, Guid.Parse(actual!.Data));
	}

	[Fact]
	public async Task 手動存款Pulsa()
	{
		// Arrange
		var channelName = Guid.NewGuid().ToString();

		var fakeBrandCertClient = Substitute.For<IBrandCertificateClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakePlayerPasswordValidateCommandService = Substitute.For<IGetService<PlayerPasswordValidateCommand, bool>>();
		var fakePlayerLoginRecordRepository = Substitute.For<IPlayerLoginRecordRepository>();
		var fakeBrandRepository = Substitute.For<IBrandRepository>();
		var fakeValidatePlayerLoginRecordCommandService = Substitute.For<ICommandService<ValidatePlayerLoginRecordCommand>>();

		var fakeCreateDepositFormService = Substitute.For<IGetService<CreateDepositFormCommand, Guid>>();
		var fakeGetBrandPaymentService = Substitute.For<IGetService<GetBrandPaymentQuery, BrandPaymentInfo>>();

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
				.AddSingleton(fakeCreateDepositFormService)
				.AddSingleton(fakeGetBrandPaymentService)
				.AddSingleton<ManualDepositSerializer>())
			.Build();

		await web.StartAsync();

		var playerCode = "test1";
		var userName = "test";

		_ = fakeBrandServiceClient.GetBrandStatusAsync(Arg.Is(brandId))
			.Returns(BrandStatus.Active);
		_ = fakePlayerServiceClient.GetPlayerInfoByUserNameAsync(Arg.Is(brandId), Arg.Is(userName))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				BrandId = brandId,
				UserName = userName,
				PlayerCode = playerCode
			});
		_ = fakePlayerPasswordValidateCommandService.GetAsync(Arg.Any<PlayerPasswordValidateCommand>())
			.Returns(true);
		_ = fakePlayerLoginRecordRepository.AddPlayerLoginRecordAsync(
				Arg.Is(brandId),
				Arg.Is(playerCode),
				Arg.Is(TGS.Core.Models.Enums.PlatformType.Desktop),
				Arg.Is("127.0.0.1"))
			.Returns(new LoginRecordInfo(
				Guid.NewGuid(),
				playerCode,
				TGS.Core.Models.Enums.LoginStatus.Enabled,
				TGS.Core.Models.Enums.PlatformType.Desktop,
				DateTimeOffset.Now,
				DateTimeOffset.Now.AddDays(1D)));

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

		var brandPaymentId = Guid.NewGuid();
		var amount = 1000m;

		var brandPaymentInfo = new BrandPaymentInfo
		{
			Id = brandPaymentId,
			ChannelId = Guid.NewGuid(),
			ChannelName = "Usdt",
			Enabled = true,
			ExtendedData = """
				{
					"phone": "0912345678"
				}
				"""
		};

		_ = fakeGetBrandPaymentService.GetAsync(
			Arg.Is<GetBrandPaymentQuery>(x => x.Id == brandPaymentId),
			Arg.Any<CancellationToken>())
			.Returns(brandPaymentInfo);

		var expected = Guid.NewGuid();

		_ = fakeCreateDepositFormService.GetAsync(
			Arg.Is<CreateDepositFormCommand>(x =>
					x.PlayerCode == playerCode
					&& x.BrandId == brandId
					&& x.BrandPaymentId == brandPaymentId),
			Arg.Any<CancellationToken>())
			.Returns(expected);

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken, amount, paymentId, promotionId, bankType, note, realname, phone) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new OperatorService(instance);

				return await svc.submitManualDepositPulsaRequest(amount, paymentId, promotionId, bankType, note, realname, phone);
			};
		""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<SuccessResponse<string>>(
			nodeModule,
			args: new object?[] { brandToken, loginResult.PlayerToken, amount, brandPaymentId, null, "pulsa", "testNote", "testRealName", "0912345678" });

		// Assert
		Assert.Equal(expected, Guid.Parse(actual!.Data));
	}
}
