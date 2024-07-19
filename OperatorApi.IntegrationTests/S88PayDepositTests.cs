using System.Runtime.InteropServices;
using System.Text.Json;
using Jering.Javascript.NodeJS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Tgs.BrandCertificate;
using Tgs.BrandCertificate.Clients;
using Tgs.Payment.Clients;
using Tgs.Payment.S88Pay.Client;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Models;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Player;
using Xunit.Abstractions;

namespace OperatorApi.IntegrationTests;

public class S88PayDepositTests(ITestOutputHelper testOutputHelper)
{
	[Fact]
	public async Task S88Pay收銀台畫面()
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
		var brandCode = "GPI";
		var brandToken = TestOperatorApplication.MakeTestBrandToken(brandId, brandCode);

		_ = fakeBrandRepository.GetSecretKeyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
			.Returns("N2KAnljo5zbLCZDSoUBFJ70ONsy");

		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakePromotionClient = Substitute.For<IPromotionClient>();
		var fakeRedisRepository = Substitute.For<IS88PayRepository>();

		using var web = TestOperatorApplication.Builder
		.UseTestChannelServer(channelName)
			.ConfigureLogging(logging => logging.AddXUnit(testOutputHelper))
			.ConfigureServices(services => services
				.AddSingleton(fakeBrandCertClient)
				.AddSingleton(fakeBrandServiceClient)
				.AddSingleton(fakePlayerServiceClient)
				.AddSingleton(fakePlayerPasswordValidateCommandService)
				.AddSingleton(fakePlayerLoginRecordRepository)
				.AddSingleton(fakeBrandRepository)
				.AddSingleton(fakeValidatePlayerLoginRecordCommandService)
				.AddSingleton(fakePaymentServiceClient)
				.AddSingleton(fakePromotionClient)
				.AddSingleton(fakeRedisRepository))
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

		var tokenId = Guid.NewGuid();

		var depositV3Response = new S88PayDepositV3Response(
			 10000m,
			Guid.NewGuid(),
			new Uri("data:image"),
			"thirdPartyOrderNo",
			DateTimeOffset.Now.AddHours(1),
			"number",
			"name",
			"matchCode",
			null);

		var data = JsonSerializer.Serialize(depositV3Response, S88PayUtility.SerializerOptions);
		_ = fakeRedisRepository.GetDepositAsync(Arg.Is(tokenId), Arg.Any<CancellationToken>())
			.Returns(data);

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken, tokenId) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new OperatorService(instance);

				return await svc.getS88PayDepositCashRegisterRequest(tokenId);
			};
			""";

		// Act
		var response = await StaticNodeJSService.InvokeFromStringAsync<string>(
			nodeModule,
			args: [brandToken, loginResult.PlayerToken, tokenId]);

		// Assert
		var actual = JsonSerializer.Deserialize<SuccessResponse<DepositCashRegisterViewModel<S88PayInfoViewModel>>>(
			response!,
			web.Services.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions);

		Assert.Equal("name", actual!.Data.PayInfo.AccountName);
		Assert.Equal("matchCode", actual!.Data.PayInfo.MatchCode);
		Assert.Equal("number", actual!.Data.PayInfo.Account);
		Assert.Equal("data:image", actual!.Data.PayInfo.Qr);
	}

	[Fact]
	public async Task S88Pay三方存款()
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
		var brandCode = "GPI";
		var brandToken = TestOperatorApplication.MakeTestBrandToken(brandId, brandCode);

		_ = fakeBrandRepository.GetSecretKeyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
			.Returns("N2KAnljo5zbLCZDSoUBFJ70ONsy");

		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakePromotionClient = Substitute.For<IPromotionClient>();
		var fakeRedisRepository = Substitute.For<IS88PayRepository>();

		using var web = TestOperatorApplication.Builder
		.UseTestChannelServer(channelName)
			.ConfigureLogging(logging => logging.AddXUnit(testOutputHelper))
			.ConfigureServices(services => services
				.AddSingleton(fakeBrandCertClient)
				.AddSingleton(fakeBrandServiceClient)
				.AddSingleton(fakePlayerServiceClient)
				.AddSingleton(fakePlayerPasswordValidateCommandService)
				.AddSingleton(fakePlayerLoginRecordRepository)
				.AddSingleton(fakeBrandRepository)
				.AddSingleton(fakeValidatePlayerLoginRecordCommandService)
				.AddSingleton(fakePaymentServiceClient)
				.AddSingleton(fakePromotionClient)
				.AddSingleton(fakeRedisRepository))
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
				    "merchantCode": "merchantCode",
				    "apiKey": "apiKey",
				    "secretKey": "secretKey",
				    "paymentCode": "paymentCode",
					"depositType": "EWallet",
				    "bankCode": "bankCode",
					"exchangeRate": 1
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
				Code = "S88Pay"
			});

		_ = fakePaymentServiceClient.LockDepositPaymentAsync(
			Arg.Any<Guid>(),
			Arg.Is("S88Pay"),
			Arg.Is(playerCode),
			Arg.Any<CancellationToken>())
			.Returns(callInfo => callInfo.ArgAt<Guid>(0));

		var applyInfo = new S88PayDepositApplyInfo(
			new S88PayDepositSettings(
				"merchantCode",
				"apiKey",
				"secretKey",
				Guid.NewGuid(),
				"paymentCode",
				"test",
				"VND",
				Enum.Parse<S88PayDepositType>("EWallet"),
				"bankCode",
				1m),
			new S88PayDepositV3Response(
				100M,
				Guid.NewGuid(),
				new Uri("http://localhost/test"),
				ThirdPartyOrderNo: "thirdPartyOrderNo",
				DateTimeOffset.Now,
				"test phone",
				"test name",
				"232168",
				null));

		_ = fakePaymentServiceClient.GetDepositFormAsync(
			Arg.Any<Guid>(),
			Arg.Any<CancellationToken>())
			.Returns(new DepositFormInfo
			{
				PlayerCode = playerCode,
				BrandId = brandId,
				ApplyInfo = JsonSerializer.Serialize(applyInfo, S88PayUtility.SerializerOptions)
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

				return await svc.submitS88ThirdPartyDepositRequest(request);
			};
			""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<SuccessResponse<DepositFormCreatedResponse>>(
			nodeModule,
			args: [brandToken, loginResult.PlayerToken, request]);

		// Assert
		Assert.True(actual?.Data.PaymentUrl.StartsWith("/deposit/transaction/s88pay?id="));

		_ = fakeRedisRepository
			.Received(1)
			.StoreDepositAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task S88Pay銀行存款()
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
		var fakeRedisRepository = Substitute.For<IS88PayRepository>();

		_ = fakeBrandCertClient.GetSystemValidateSecurityKeyAsync(Arg.Any<CancellationToken>())
			.Returns(TestOperatorApplication.TestSecurityKey);

		var brandId = Guid.NewGuid();
		var brandCode = "GPI";
		var brandToken = TestOperatorApplication.MakeTestBrandToken(brandId, brandCode);

		_ = fakeBrandRepository.GetSecretKeyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
			.Returns("N2KAnljo5zbLCZDSoUBFJ70ONsy");

		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakePromotionClient = Substitute.For<IPromotionClient>();

		using var web = TestOperatorApplication.Builder
		.UseTestChannelServer(channelName)
			.ConfigureLogging(logging => logging.AddXUnit(testOutputHelper))
			.ConfigureServices(services => services
				.AddSingleton(fakeBrandCertClient)
				.AddSingleton(fakeBrandServiceClient)
				.AddSingleton(fakePlayerServiceClient)
				.AddSingleton(fakePlayerPasswordValidateCommandService)
				.AddSingleton(fakePlayerLoginRecordRepository)
				.AddSingleton(fakeBrandRepository)
				.AddSingleton(fakeValidatePlayerLoginRecordCommandService)
				.AddSingleton(fakePaymentServiceClient)
				.AddSingleton(fakePromotionClient)
				.AddSingleton(fakeRedisRepository))
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
				    "merchantCode": "merchantCode",
				    "apiKey": "apiKey",
				    "secretKey": "secretKey",
				    "paymentCode": "paymentCode",
					"depositType": "Bank",
				    "bankCode": "bankCode",
					"exchangeRate": 1
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
				Code = "S88Pay"
			});

		_ = fakePaymentServiceClient.LockDepositPaymentAsync(
			Arg.Any<Guid>(),
			Arg.Is("S88Pay"),
			Arg.Is(playerCode),
			Arg.Any<CancellationToken>())
			.Returns(callInfo => callInfo.ArgAt<Guid>(0));

		var s88Response = new S88PayDepositV3Response(
				100M,
				Guid.NewGuid(),
				new Uri("http://localhost/test"),
				ThirdPartyOrderNo: "DP1721182157524",
				DateTimeOffset.Now,
				"123456789",
				"TESTING bankCode ACCOUNT",
				"DP1721182157524",
				"ACB");

		var applyInfo = new S88PayDepositApplyInfo(
			new S88PayDepositSettings(
				"merchantCode",
				"apiKey",
				"secretKey",
				Guid.NewGuid(),
				"paymentCode",
				"test",
				"VND",
				Enum.Parse<S88PayDepositType>("Bank"),
				"bankCode",
				1m),
			s88Response);

		_ = fakePaymentServiceClient.GetDepositFormAsync(
			Arg.Any<Guid>(),
			Arg.Any<CancellationToken>())
			.Returns(new DepositFormInfo
			{
				PlayerCode = playerCode,
				BrandId = brandId,
				ApplyInfo = JsonSerializer.Serialize(applyInfo, S88PayUtility.SerializerOptions)
			});

		var expected = new DepositFormCreatedResponse
		{
			FormId = Guid.NewGuid(),
			PaymentUrl = "/deposit/transaction/s88pay?id=*"
		};

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken, request) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new OperatorService(instance);

				return await svc.submitS88BankDepositRequest(request);
			};
			""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<SuccessResponse<DepositFormCreatedResponse>>(
			nodeModule,
			args: [brandToken, loginResult.PlayerToken, request]);

		// Assert
		Assert.True(actual?.Data.PaymentUrl.StartsWith("/deposit/transaction/s88pay?id="));

		_ = fakeRedisRepository
			.Received(1)
			.StoreDepositAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
	}
}