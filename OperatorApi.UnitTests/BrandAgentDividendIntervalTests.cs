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
public class BrandAgentDividendIntervalTests
{
	[Test]
	public async Task Can_Get_Brand_Agent_Dividend_Interval()
	{
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeMemoryCache = Substitute.For<IMemoryCache>();

		var sut = new GetBrandAgentDividendInfoQueryService(fakeBrandServiceClient, fakeMemoryCache);

		_ = fakeBrandServiceClient.GetBrandFullInfoAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
			.Returns(new BrandFullInfo());

		var brandId = Guid.NewGuid();

		var query = new GetBrandAgentDividendInfoQuery(brandId);

		_ = await sut.GetAsync(query);

		_ = fakeBrandServiceClient.Received(1)
			.GetBrandFullInfoAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
	}
}
