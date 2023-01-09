using System.Runtime.InteropServices;
using Jering.Javascript.NodeJS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tgs.BrandCertificate.Clients;
using TGS.Apis.OperatorApi.Models;
using TGS.Services.Operator;
using TGS.Services.Operator.GameProviders;
using Xunit.Abstractions;

namespace OperatorApi.IntegrationTests;

public class GameProviderTests
{
	private readonly ITestOutputHelper _testOutputHelper;

	public GameProviderTests(ITestOutputHelper testOutputHelper)
	{
		_testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
	}

	[Fact]
	public async Task 取得在維護的GP列表()
	{
		// Arrange
		var channelName = Guid.NewGuid().ToString();

		var fakeBrandCertClient = Substitute.For<IBrandCertificateClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();

		_ = fakeBrandCertClient.GetSystemValidateSecurityKeyAsync(Arg.Any<CancellationToken>())
			.Returns(TestOperatorApplication.TestSecurityKey);

		var brandId = Guid.NewGuid();
		var brandCode = "Test";
		var brandToken = TestOperatorApplication.MakeTestBrandToken(brandId, brandCode);

		using var web = TestOperatorApplication.Builder
			.UseTestChannelServer(channelName)
			.ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper))
			.ConfigureServices(services => services
				.AddSingleton(fakeBrandCertClient)
				.AddSingleton(fakeBrandServiceClient)
				.AddSingleton(fakeGameProviderRepository))
			.Build();

		await web.StartAsync();

		var expectedSubGameProviders = new[] {
			new GameProviderStatus("Game1", TGS.Core.Models.Enums.Status.Maintenance),
			new GameProviderStatus("Game2", TGS.Core.Models.Enums.Status.Maintenance)
		};

		_ = fakeGameProviderRepository.ListNotActiveSubGameProvidersAsync(
			Arg.Is(brandId),
			Arg.Any<CancellationToken>())
			.Returns(expectedSubGameProviders.ToAsyncEnumerable());

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";

		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const BackOfficeService = require('./services/op-service.js').default;

			module.exports = async (brandToken) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken);

				let svc = new BackOfficeService(instance);

				return await svc.getMaintainGameProviders();
			};
		""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<SuccessResponse<GameProviderStatus[]>>(
			nodeModule,
			args: new object?[] { brandToken });

		// Assert
		Assert.Equal(expectedSubGameProviders, actual?.Data);
	}
}
