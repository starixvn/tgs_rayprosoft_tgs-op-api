using System.Collections.Immutable;
using System.Runtime.InteropServices;
using ExpectedObjects;
using Jering.Javascript.NodeJS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tgs.BrandCertificate;
using Tgs.BrandCertificate.Clients;
using Tgs.BrandGameCredits.Common;
using Tgs.CashService.Client;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Models;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Brand;
using TGS.Services.Operator.ApplicationServices.Player;
using Xunit.Abstractions;
using TransactionLogResponseViewMode = TGS.Apis.OperatorApi.Models.Messages.PagingResponse<TGS.Apis.OperatorApi.Models.Messages.TransactionLogViewModel>;

namespace OperatorApi.IntegrationTests;

public class TransactionLogTests(ITestOutputHelper testOutputHelper)
{
	private readonly ITestOutputHelper _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));

	[Fact]
	public async Task 查詢玩家的帳變紀錄()
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
		var fakeBrandCreditQueryService = Substitute.For<IGetService<BrandCreditQuery, BrandGameCreditRiskTypes>>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();

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
				.AddSingleton(fakeBrandCreditQueryService)
				.AddSingleton(fakeCashServiceClient))
			.Build();

		await web.StartAsync();

		var from = DateTime.Now.AddDays(-1D);
		var to = DateTime.Now.AddDays(1D);

		var playerCode = "test1";
		var userName = "test";
		var walletId = Guid.NewGuid();

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

		var expected = new TransactionLogResponseViewMode
		{
			TotalCount = 2,
			Items = new[]
			{
				new TransactionLogViewModel
				{
					Id  = Guid.NewGuid(),
					Amount = 100,
					RecordedOn = DateTime.UtcNow,
					TransactionType = "Transaction.Promotion"
				},
				new TransactionLogViewModel {
					Id = Guid.NewGuid(),
					Amount = 200,
					RecordedOn = DateTime.UtcNow,
					TransactionType = "Transaction.Rebate"
				}
			}
		};

		_ = fakePlayerServiceClient.GetPlayerPropertiesAsync(Arg.Is(playerCode), Arg.Any<CancellationToken>())
			.Returns(new Dictionary<string, string>
			{
				["DefaultWalletId"] = walletId.ToString()
			}.ToImmutableDictionary());

		_ = fakeCashServiceClient.QueryTransactionLogsAsync(
			Arg.Is(walletId),
			Arg.Is<DateTime>(x => x.ToUniversalTime() == from.ToUniversalTime()),
			Arg.Is<DateTime>(x => x.ToUniversalTime() == to.ToUniversalTime()),
			Arg.Any<IEnumerable<int>>(),
			Arg.Is(new PagingConditions(1, 10)),
			Arg.Any<CancellationToken>())
			.Returns(new Tgs.CashService.Client.PagingResponse<TransactionLog>(
				expected.Items.Select(v => new TransactionLog(
					v.Id,
					v.RecordedOn,
					v.TransactionType switch
					{
						"Transaction.Promotion" => 6,
						"Transaction.Rebate" => 4,
						_ => 0
					},
					v.Amount,
					0M,
					0M,
					0M,
					null,
					null)),
				1,
				2));

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";

		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken, from, to, page, rows) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new OperatorService(instance);

				return await svc.getPlayerTransactionLogs(from, to, page, rows);
			};
		""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<SuccessResponse<TransactionLogResponseViewMode>>(
			nodeModule,
			args: [brandToken, loginResult.PlayerToken, from, to, 1, 10]);

		// Assert
		Assert.Equal(expected.TotalCount, actual?.Data.TotalCount);
		expected.Items.ToExpectedObject().ShouldMatch(actual?.Data.Items);
	}
}
