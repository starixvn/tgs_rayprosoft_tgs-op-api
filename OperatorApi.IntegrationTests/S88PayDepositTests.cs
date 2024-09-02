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
			S88PayDepositType.EWallet,
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
}