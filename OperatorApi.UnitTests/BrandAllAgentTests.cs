using System;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.BrandAllAgent;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class BrandAllAgentTests
{
	[Test]
	public async Task Can_Get_Brand_All_Agent()
	{
		var fakeBrandRepository = Substitute.For<IBrandRepository>();

		var sut = new GetBrandAllAgentQueryService(fakeBrandRepository);

		var brandId = Guid.NewGuid();

		var query = new GetBrandAllAgentQuery(brandId);

		_ = await sut.GetAsync(query);

		_ = fakeBrandRepository.Received(1)
			.CheckHaveAllAgentAsync(Arg.Is(brandId));
	}
}
