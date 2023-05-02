using System.Runtime.InteropServices;
using ExpectedObjects;
using Jering.Javascript.NodeJS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Tgs.BrandCertificate;
using Tgs.BrandCertificate.Clients;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using Tgs.StationLetterService.Client;
using TGS.Apis.OperatorApi.Models;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Player;
using Xunit.Abstractions;

namespace OperatorApi.IntegrationTests;

using PagingStationLetterListView = TGS.Apis.OperatorApi.Models.Messages.PagingResponse<StationLetterListItemViewModel>;

public class StationLetterTests
{
	private readonly ITestOutputHelper _testOutputHelper;

	public StationLetterTests(ITestOutputHelper testOutputHelper)
	{
		_testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
	}

	[Fact]
	public async Task 取得站內信內容()
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
		var fakeStationLetterClient = Substitute.For<IStationLetterClient>();

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
				.AddSingleton(fakeStationLetterClient))
			.Build();

		await web.StartAsync();

		var playerCode = "test1";
		var userName = "test";
		var letterId = Guid.NewGuid();

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

		var letter = new StationLetter(
			letterId,
			brandCode,
			playerCode,
			"ttt",
			null,
			"test123456",
			null,
			DateTime.UtcNow,
			false,
			false);

		_ = fakeStationLetterClient.GetStationLetterAsync(
			Arg.Is(letterId),
			Arg.Any<CancellationToken>())
			.Returns(letter);

		var expected = new StationLetterViewModel
		{
			Id = letter.Id,
			Sender = letter.Sender,
			Subject = letter.Subject,
			SentOn = letter.SentOn,
			Content = letter.Content,
			HaveRead = letter.HaveRead,
			Receiver = letter.Receiver,
			Replied = letter.Replied,
			Quotation = letter.Quotation
		};

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken, letterId) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new OperatorService(instance);

				return await svc.getPlayerStationLetterContent(letterId);
			};
			""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<SuccessResponse<StationLetterViewModel?>>(
			nodeModule,
			args: new object?[] { brandToken, loginResult.PlayerToken, letterId });

		// Assert
		Assert.NotNull(actual);
		expected.ToExpectedObject().ShouldEqual(actual.Data);
	}

	[Fact]
	public async Task 取得站內信列表()
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
		var fakeStationLetterClient = Substitute.For<IStationLetterClient>();

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
				.AddSingleton(fakeStationLetterClient))
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

		var letter = new StationLetter(
			Guid.NewGuid(),
			brandCode,
			playerCode,
			"ttt",
			null,
			"test123456",
			null,
			DateTime.UtcNow,
			false,
			false);

		_ = fakeStationLetterClient.ListStationLettersAsync(Arg.Is(playerCode), Arg.Any<CancellationToken>())
			.Returns(new[] { letter }.ToAsyncEnumerable());

		var expected = new StationLetterListItemViewModel
		{
			Id = letter.Id,
			Sender = letter.Sender,
			Subject = letter.Subject,
			SentOn = letter.SentOn
		};

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken, page, rows) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new OperatorService(instance);

				return await svc.getPlayerStationLetters(page, rows);
			};
			""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<SuccessResponse<PagingStationLetterListView>>(
			nodeModule,
			args: new object?[] { brandToken, loginResult.PlayerToken, 1, 10 });

		// Assert
		Assert.NotNull(actual);
		Assert.Equal(1, actual.Data.TotalCount);
		expected.ToExpectedObject().ShouldEqual(actual.Data.Items.First());
	}

	[Fact]
	public async Task 發送站內信給客服()
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
		var fakeStationLetterClient = Substitute.For<IStationLetterClient>();

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
				.AddSingleton(fakeStationLetterClient))
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

		var request = new CreateStationLetterViewModel
		{
			Subject = "tttt",
			Content = "ccccc"
		};

		_ = fakeBrandServiceClient.GetBrandInfoAsync(
			Arg.Is(brandId),
			Arg.Any<CancellationToken>())
			.Returns(new Tgs.BrandCertificate.Clients.BrandInfo
			{
				Id = brandId,
				Code = brandCode,
				Currency = "CNY"
			});
		_ = fakeBrandServiceClient.GetBrandFrontendInfoAsync(Arg.Is(brandId), Arg.Any<CancellationToken>())
			.Returns(new BrandFrontendInfo(null, null, null, null));

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken, request) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new OperatorService(instance);

				return await svc.sendStationLetterToCustomService(request);
			};
			""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<SuccessResponse<object?>>(
			nodeModule,
			args: new object?[] { brandToken, loginResult.PlayerToken, request });

		// Assert
		Assert.NotNull(actual);
		Assert.Equal(0, actual.Error);

		var requestExpected = new StationLetterRequest(
			playerCode,
			new[] { brandCode },
			request.Subject,
			request.Content).ToExpectedObject(ctx =>
			{
				_ = ctx.IgnoreEqualsOverride();
				_ = ctx.ArrayMemberShouldEqual(o => o.Receivers);
			});
		_ = fakeStationLetterClient.Received(1)
			.SendStationLetterAsync(
				Arg.Is<StationLetterRequest>(req => requestExpected.Equals(req)),
				Arg.Any<CancellationToken>());
	}
}
