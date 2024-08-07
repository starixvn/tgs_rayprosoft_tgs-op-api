using Tgs.BrandCertificate.Clients;
using TGS.Services.Operator.ApplicationServices.Brand;

namespace TGS.Tests.OperatorApi.UnitTests;

public class BrandPlayerContractDisplayQueryServiceTests
{
	[Test]
	public async Task BrandPlayerContractDisplayQueryService_取得聯繫信息設定()
	{
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var sut = new BrandPlayerContractDisplayQueryService(fakeBrandServiceClient);

		var query = new BrandPlayerContractDisplayQuery(Guid.NewGuid());

		_ = await sut.QueryAsync(query).ToArrayAsync();

		_ = fakeBrandServiceClient.Received(1)
			.ListBrandPlayerContractDisplayAsync(Arg.Is(query.BrandId));
	}
}