using Tgs.BrandCertificate.Clients;
using TGS.Services.Operator.ApplicationServices.Brand;

namespace TGS.Tests.OperatorApi.UnitTests;
public class ListSocialMediaSettingsQueryServiceTests
{
	[Test]
	public async Task ListSocialMediaSettingsQueryService_取得社群連結列表資料()
	{
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var sut = new ListSocialMediaSettingsQueryService(fakeBrandServiceClient);

		var query = new ListSocialMediaSettingsQuery(Guid.NewGuid());

		_ = await sut.QueryAsync(query).ToArrayAsync();

		_ = fakeBrandServiceClient.Received(1)
			.ListSocialMediaSettingsAsync(Arg.Is(query.BrandId));
	}
}
