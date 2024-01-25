using System;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Tgs.Paging;
using TGS.Core.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Vip;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class VipTests
{
	[Test]
	public async Task Can_Get_Vip_Levels_Count()
	{
		var fakeVipLevelRepository = Substitute.For<IVipLevelRepository>();

		var sut = new CountVipLevelsQueryService(fakeVipLevelRepository);

		var query = new GetVipLevelListQuery(
			Guid.NewGuid(),
			new Paging<VipRecord>());

		_ = await sut.GetAsync(query);

		_ = fakeVipLevelRepository.Received(1)
			.QueryVipLevelsTotalCountAsync(query.BrandId);
	}

	[Test]
	public async Task Can_List_Vip_Levels()
	{
		var fakeVipLevelRepository = Substitute.For<IVipLevelRepository>();

		var sut = new GetVipLevelListQueryService(fakeVipLevelRepository);

		var query = new GetVipLevelListQuery(
			Guid.NewGuid(),
			new Paging<VipRecord>());

		_ = await sut.QueryAsync(query).ToArrayAsync();

		_ = fakeVipLevelRepository
			.Received(1)
			.QueryVipLevelsAsync(
				Arg.Is(query.BrandId),
				Arg.Any<Paging<VipRecord>>());
	}
}
