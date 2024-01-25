using System.Runtime.InteropServices;
using ExpectedObjects;
using Jering.Javascript.NodeJS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tgs.BrandCertificate;
using Tgs.BrandCertificate.Clients;
using Tgs.PlayerService.Client;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Models;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Player;
using Xunit.Abstractions;

namespace OperatorApi.IntegrationTests;

public class PlayerBucketTests(ITestOutputHelper testOutputHelper)
{
	private readonly ITestOutputHelper _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));

	[Fact]
	public async Task 取得玩家的水桶餘額()
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

		var fakePlayerBucketClient = Substitute.For<IPlayerBucketClient>();

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
				.AddSingleton(fakePlayerBucketClient))
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

		_ = fakePlayerBucketClient.ReadBucketsAsync(
			Arg.Is(playerCode),
			Arg.Any<CancellationToken>())
			.Returns(new ReadBucketResult(1000M, Guid.NewGuid()));

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new OperatorService(instance);

				return await svc.getWithdrawRequireBucketAmount();
			};
			""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<SuccessResponse<decimal?>>(
			nodeModule,
			args: [brandToken, loginResult.PlayerToken]);

		// Assert
		Assert.Equal(1000M, actual?.Data);
	}

	[Fact]
	public async Task 取得玩家遊戲水桶餘額列表()
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

		var fakePlayerBucketClient = Substitute.For<IPlayerBucketClient>();

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
				.AddSingleton(fakePlayerBucketClient))
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

		_ = fakePlayerBucketClient.GetBrandBucketTypesAsync(
			Arg.Is(brandId),
			Arg.Any<CancellationToken>())
			.Returns(new[] { "Game1", "Game2" }.ToAsyncEnumerable());

		_ = fakePlayerBucketClient.ReadBucketsAsync(
			Arg.Is(playerCode),
			Arg.Any<CancellationToken>())
			.Returns(new ReadBucketResult(1000M, Guid.NewGuid()));
		_ = fakePlayerBucketClient.ReadBucketsAsync(
			Arg.Is($"{playerCode}:Game1"),
			Arg.Any<CancellationToken>())
			.Returns(new ReadBucketResult(2000M, Guid.NewGuid()));
		_ = fakePlayerBucketClient.ReadBucketsAsync(
			Arg.Is($"{playerCode}:Game2"),
			Arg.Any<CancellationToken>())
			.Returns(new ReadBucketResult(3000M, Guid.NewGuid()));

		var expected = new[]
		{
			new TypeBucketResultViewModel
			{
				BucketType = null,
				Amount = 1000M
			},
			new TypeBucketResultViewModel
			{
				BucketType = "Game1",
				Amount = 2000M
			},
			new TypeBucketResultViewModel
			{
				BucketType = "Game2",
				Amount = 3000M
			}
		};

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new OperatorService(instance);

				return await svc.getAllTypeBucketAmounts();
			};
			""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<SuccessResponse<TypeBucketResultViewModel[]>>(
			nodeModule,
			args: [brandToken, loginResult.PlayerToken]);

		// Assert
		Assert.NotNull(actual);
		expected.ToExpectedObject().ShouldMatch(actual.Data);
	}
}
