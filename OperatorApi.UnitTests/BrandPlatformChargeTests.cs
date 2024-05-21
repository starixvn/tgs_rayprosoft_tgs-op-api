using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Tgs.BrandCertificate.Clients;
using Tgs.Player.All.Agent.Client;
using Tgs.Player.All.Agent.Client.Models;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Brand;
using TGS.Services.Operator.ApplicationServices.BrandAllAgent;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class BrandPlatformChargeTests
{
	[Test]
	public async Task Can_Get_Brand_All_Agent_Switch_On()
	{
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeMemoryCache = Substitute.For<IMemoryCache>();

		var sut = new GetPlatformChargeQueryService(fakeBrandServiceClient, fakeMemoryCache);

		_ = fakeBrandServiceClient.GetBrandFullInfoAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
			.Returns(new BrandFullInfo());

		var brandId = Guid.NewGuid();

		var query = new GetPlatformChargeQuery(brandId);

		_ = await sut.GetAsync(query);

		_ = fakeBrandServiceClient.Received(1)
			.GetBrandFullInfoAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
	}
}
