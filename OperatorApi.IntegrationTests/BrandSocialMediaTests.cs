using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Jering.Javascript.NodeJS;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using TGS.Apis.OperatorApi.Models;
using Tgs.BrandCertificate.Clients;
using Tgs.PlayerService.Client;
using Tgs.Service.PlayerBuckets;
using TGS.Services.Operator.ApplicationServices.Player;
using TGS.Services.Operator;
using Tgs.ServiceUtility;
using Xunit;
using Xunit.Abstractions;
using Tgs.BrandCertificate;
using TGS.Apis.OperatorApi.Models.Messages;

namespace OperatorApi.IntegrationTests;
public class BrandSocialMediaTests(ITestOutputHelper testOutputHelper)
{
	[Fact]
	public async Task 整合測試_取得社群連結列表()
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
				.AddSingleton(fakeValidatePlayerLoginRecordCommandService))
			.Build();

		await web.StartAsync();

		_ = fakeBrandServiceClient.GetBrandStatusAsync(
			Arg.Is(brandId),
			Arg.Any<CancellationToken>())
			.Returns(BrandStatus.Active);

		_ = fakeBrandServiceClient.ListSocialMediaSettingsAsync(
			Arg.Is(brandId),
			Arg.Any<CancellationToken>())
			.Returns(new[]
			{
				new SocialMediaSetting(
					Guid.NewGuid(),
					"Test1",
					SocialType.Facebook,
					new Uri("https://localhost"),
					100),
				new SocialMediaSetting(
					Guid.NewGuid(),
					"Test2",
					SocialType.Facebook,
					new Uri("https://localhost"),
					200),
				new SocialMediaSetting(
					Guid.NewGuid(),
					"Test3",
					SocialType.Facebook,
					new Uri("https://localhost"),
					300)
			}.ToAsyncEnumerable());

		var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "true" : "false";
		var nodeModule = $$"""
			const axiosConfigFactory = require('./models/axios-configuration.js').default;
			const OperatorService = require('./services/op-service.js').default;

			module.exports = async (brandToken, loginToken) =>
			{
				let instance = axiosConfigFactory("{{channelName}}", {{isWindows}}, brandToken, loginToken);

				let svc = new OperatorService(instance);

				return await svc.listBrandSocialMediaSettings();
			};
			""";

		// Act
		var actual = await StaticNodeJSService.InvokeFromStringAsync<SuccessResponse<SocialMediaSettingViewModel[]>>(
			nodeModule,
			args: [brandToken]);

		// Assert
		Assert.NotNull(actual);
		Assert.Equal(3, actual.Data.Length);
	}
}
