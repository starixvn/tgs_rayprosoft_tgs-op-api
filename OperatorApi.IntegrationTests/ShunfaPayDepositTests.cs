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
using Tgs.Payment.ShunfaPay.Client;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Models;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Player;
using Xunit.Abstractions;

namespace OperatorApi.IntegrationTests;

public class ShunfaPayDepositTests(ITestOutputHelper testOutputHelper)
{
	[Fact]
	public async Task ShunfaPay三方存款()
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
					"clientId": "clientId",
					"apiKey": "apiKey",
					"type": "02"
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
				Code = "ShunfaPay"
			});

		_ = fakePaymentServiceClient.LockDepositPaymentAsync(
			Arg.Any<Guid>(),
			Arg.Is("ShunfaPay"),
			Arg.Is(playerCode),
			Arg.Any<CancellationToken>())
			.Returns(callInfo => callInfo.ArgAt<Guid>(0));

		var applyInfo = new ShunfaPayDepositApplyInfo(
			new ShunfaPayDepositSettings(
				"clientId",
				"apiKey",
				Guid.NewGuid(),
				"02"),
			new ShunfaPayDepositResponse(
				100M,
				Guid.NewGuid(),
				new Uri("http://localhost/test"),
				ThirdPartyOrderNo: "130d36a1176d42638948161df47f160f"));

		_ = fakePaymentServiceClient.GetDepositFormAsync(
			Arg.Any<Guid>(),
			Arg.Any<CancellationToken>())
			.Returns(new DepositFormInfo
			{
				PlayerCode = playerCode,
				BrandId = brandId,
				ApplyInfo = JsonSerializer.Serialize(applyInfo, ShunfaPayUtility.SerializerOptions)
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

				return await svc.submitShunfaThirdPartyDepositRequest(request);
			};
			""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<SuccessResponse<DepositFormCreatedResponse>>(
			nodeModule,
			args: [brandToken, loginResult.PlayerToken, request]);

		// Assert
		Assert.Equal(applyInfo.Response.RedirectUrl.ToString(), actual?.Data.PaymentUrl);
	}
}