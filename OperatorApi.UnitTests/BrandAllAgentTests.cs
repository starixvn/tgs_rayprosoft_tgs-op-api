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
	public async Task Can_Get_Brand_All_Agent_Switch_On()
	{
		var fakeBrandRepository = Substitute.For<IBrandRepository>();

		var sut = new GetBrandAllAgentSwitchOnQueryService(fakeBrandRepository);

		var brandId = Guid.NewGuid();

		var query = new GetBrandAllAgentSwitchOnQuery(brandId);

		_ = await sut.GetAsync(query);

		_ = fakeBrandRepository.Received(1)
			.CheckHaveAllAgentAsync(Arg.Is(brandId));
	}

	[Test]
	public void Can_Get_Brand_All_Agent()
	{
		var fakeBrandRepository = Substitute.For<IBrandRepository>();

		var sut = new GetBrandAllAgentTurnoverConditionsQueryService(fakeBrandRepository);

		var brandId = Guid.NewGuid();

		var query = new GetBrandAllAgentTurnoverConditionsQuery(brandId);

		_ = sut.QueryAsync(query);

		_ = fakeBrandRepository.Received(1)
			.GetBrandAllAgentTurnoverConditionsAsync(Arg.Is(brandId));
	}
}
