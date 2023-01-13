using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tgs.Payment.Clients;
using TGS.Services.Operator.ApplicationServices.Deposit;
using Tgs.ServiceUtility;
using Xunit.Abstractions;
using TGS.Payment.ManualDeposit;
using ExpectedObjects;
using Jering.Javascript.NodeJS;
using TGS.Apis.OperatorApi.Models;
using Grpc.Core;
using System.Runtime.InteropServices;
using TGS.Apis.OperatorApi.Models.Messages;
using Tgs.BrandCertificate;
using Tgs.BrandCertificate.Clients;
using Tgs.CashService.Client;
using TGS.Core.DataServices.Tgs.Models;
using Tgs.PlayerService.Client;
using TGS.Services.Operator.ApplicationServices.Player;
using TGS.Services.Operator;

namespace OperatorApi.IntegrationTests;

public class DepositTests
{
	private readonly ITestOutputHelper _testOutputHelper;

	public DepositTests(ITestOutputHelper testOutputHelper)
	{
		_testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
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
			const BackOfficeService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken, amount, paymentId, promotionId, bankType) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new BackOfficeService(instance);

				return await svc.submitDepositUsdt(amount, paymentId, promotionId, bankType);
			};
		""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<SuccessResponse<string>>(
			nodeModule,
			args: new object?[] { brandToken, loginResult.PlayerToken, amount, brandPaymentId, null, "usdt" });

		// Assert
		Assert.Equal(expected, Guid.Parse(actual!.Data));
	}
}
