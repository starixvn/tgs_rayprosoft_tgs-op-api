using System.Runtime.InteropServices;
using ExpectedObjects;
using Jering.Javascript.NodeJS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Tgs.BrandCertificate;
using Tgs.BrandCertificate.Clients;
using Tgs.Payment.Clients;
using Tgs.PlayerService.Client;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Models;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Player;
using TGS.Services.Operator.ApplicationServices.Withdraw;
using Xunit;
using Xunit.Abstractions;

namespace OperatorApi.IntegrationTests;

public class WithdrawTests
{
	private readonly ITestOutputHelper _testOutputHelper;

	public WithdrawTests(ITestOutputHelper testOutputHelper)
	{
		_testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
	}

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

				return await svc.getWithdrawRequireBucketAmountOld();
			};
			""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<SuccessResponse<decimal?>>(
			nodeModule,
			args: new object?[] { brandToken, loginResult.PlayerToken });

		// Assert
		Assert.Equal(1000M, actual?.Data);
	}

	[Fact]
	public async Task 玩家建立提款單()
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
		var fakeCheckIdentityService = Substitute.For<IGetService<CheckPlayerIdentityCommand, bool>>();
		var fakeCheckCanCreateService = Substitute.For<IGetService<CreateWithdrawFormCommand, bool>>();
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

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
				.AddSingleton(fakePlayerBucketClient)
				.AddSingleton(fakeCheckIdentityService)
				.AddSingleton(fakeCheckCanCreateService)
				.AddSingleton(fakePaymentServiceClient))
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

		var request = new CreateWithdrawRequest
		{
			Amount = 1000M,
			BankInfoId = Guid.NewGuid()
		};

		_ = fakeCheckIdentityService.GetAsync(
			Arg.Any<CheckPlayerIdentityCommand>(),
			Arg.Any<CancellationToken>())
			.Returns(true);

		_ = fakeCheckCanCreateService.GetAsync(
			Arg.Any<CreateWithdrawFormCommand>(),
			Arg.Any<CancellationToken>())
			.Returns(true);

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(
			Arg.Is(playerCode),
			Arg.Any<CancellationToken>())
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				PlayerCode = playerCode,
				BrandId = brandId,
				UserName = userName,
			});

		_ = fakePlayerBucketClient.ReadBucketsAsync(
			Arg.Is(playerCode),
			Arg.Any<CancellationToken>())
			.Returns(new ReadBucketResult(0M, null));

		var paymentRequest = new WithdrawFormCreateInfo
		{
			Amount = request.Amount,
			BrandId = brandId,
			BankInfoId = request.BankInfoId,
			UserId = playerCode,
			UserName = userName,
			UserType = UserType.Player,
			Note = request.Note,
		};

		var expectedId = Guid.NewGuid();

		_ = fakePaymentServiceClient.CreateWithdrawFormAsync(
			Arg.Is<WithdrawFormCreateInfo>(
				req => paymentRequest.ToExpectedObject().Equals(paymentRequest)),
			Arg.Any<CancellationToken>())
			.Returns(expectedId);

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken, request) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new OperatorService(instance);

				return await svc.createWithdraw(request);
			};
			""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<SuccessResponse<Guid?>>(
			nodeModule,
			args: new object?[] { brandToken, loginResult.PlayerToken, request });

		// Assert
		Assert.NotNull(actual);
		Assert.Equal(expectedId, actual!.Data);

		_ = fakePaymentServiceClient.Received(1)
			.SubmitWithdrawFormStatusAsync(
				Arg.Is(expectedId),
				Arg.Is(PaymentFlowState.WaitApprove),
				cancellationToken: Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task 玩家建立提款單_PaymentService回應InsufficientFundsException()
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
		var fakeCheckIdentityService = Substitute.For<IGetService<CheckPlayerIdentityCommand, bool>>();
		var fakeCheckCanCreateService = Substitute.For<IGetService<CreateWithdrawFormCommand, bool>>();
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

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
				.AddSingleton(fakePlayerBucketClient)
				.AddSingleton(fakeCheckIdentityService)
				.AddSingleton(fakeCheckCanCreateService)
				.AddSingleton(fakePaymentServiceClient))
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

		var request = new CreateWithdrawRequest
		{
			Amount = 1000M,
			BankInfoId = Guid.NewGuid()
		};

		_ = fakeCheckIdentityService.GetAsync(
			Arg.Any<CheckPlayerIdentityCommand>(),
			Arg.Any<CancellationToken>())
			.Returns(true);

		_ = fakeCheckCanCreateService.GetAsync(
			Arg.Any<CreateWithdrawFormCommand>(),
			Arg.Any<CancellationToken>())
			.Returns(true);

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(
			Arg.Is(playerCode),
			Arg.Any<CancellationToken>())
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				PlayerCode = playerCode,
				BrandId = brandId,
				UserName = userName,
			});

		_ = fakePlayerBucketClient.ReadBucketsAsync(
			Arg.Is(playerCode),
			Arg.Any<CancellationToken>())
			.Returns(new ReadBucketResult(0M, null));

		var paymentRequest = new WithdrawFormCreateInfo
		{
			Amount = request.Amount,
			BrandId = brandId,
			BankInfoId = request.BankInfoId,
			UserId = playerCode,
			UserName = userName,
			UserType = UserType.Player,
			Note = request.Note,
		};

		_ = fakePaymentServiceClient.CreateWithdrawFormAsync(
			Arg.Is<WithdrawFormCreateInfo>(
				req => paymentRequest.ToExpectedObject().Equals(paymentRequest)),
			Arg.Any<CancellationToken>())
			.ThrowsAsync(new InsufficientFundsException());

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken, request) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new OperatorService(instance);
				try{
					return await svc.createWithdraw(request);
				}
				catch(error){
					return error.response.data;
				}
			};
			""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<FailResponse>(
			nodeModule,
			args: new object?[] { brandToken, loginResult.PlayerToken, request });

		// Assert
		Assert.NotNull(actual);
		Assert.Equal((int)OpApiError.InsufficientFunds, actual.Error);
	}

	[Fact]
	public async Task 玩家建立提款單_PayamentService回應InsufficientBucketException()
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
		var fakeCheckIdentityService = Substitute.For<IGetService<CheckPlayerIdentityCommand, bool>>();
		var fakeCheckCanCreateService = Substitute.For<IGetService<CreateWithdrawFormCommand, bool>>();
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

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
				.AddSingleton(fakePlayerBucketClient)
				.AddSingleton(fakeCheckIdentityService)
				.AddSingleton(fakeCheckCanCreateService)
				.AddSingleton(fakePaymentServiceClient))
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

		var request = new CreateWithdrawRequest
		{
			Amount = 1000M,
			BankInfoId = Guid.NewGuid()
		};

		_ = fakeCheckIdentityService.GetAsync(
			Arg.Any<CheckPlayerIdentityCommand>(),
			Arg.Any<CancellationToken>())
			.Returns(true);

		_ = fakeCheckCanCreateService.GetAsync(
			Arg.Any<CreateWithdrawFormCommand>(),
			Arg.Any<CancellationToken>())
			.Returns(true);

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(
			Arg.Is(playerCode),
			Arg.Any<CancellationToken>())
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				PlayerCode = playerCode,
				BrandId = brandId,
				UserName = userName,
			});

		_ = fakePlayerBucketClient.ReadBucketsAsync(
			Arg.Is(playerCode),
			Arg.Any<CancellationToken>())
			.Returns(new ReadBucketResult(0M, null));

		var paymentRequest = new WithdrawFormCreateInfo
		{
			Amount = request.Amount,
			BrandId = brandId,
			BankInfoId = request.BankInfoId,
			UserId = playerCode,
			UserName = userName,
			UserType = UserType.Player,
			Note = request.Note,
		};

		_ = fakePaymentServiceClient.CreateWithdrawFormAsync(
			Arg.Is<WithdrawFormCreateInfo>(
				req => paymentRequest.ToExpectedObject().Equals(paymentRequest)),
			Arg.Any<CancellationToken>())
			.ThrowsAsync(new InsufficientBucketException());

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken, request) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new OperatorService(instance);
				try{
					return await svc.createWithdraw(request);
				}
				catch(error){
					return error.response.data;
				}
			};
			""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<FailResponse>(
			nodeModule,
			args: new object?[] { brandToken, loginResult.PlayerToken, request });

		// Assert
		Assert.NotNull(actual);
		Assert.Equal((int)OpApiError.InsufficientBucket, actual.Error);
	}
}
