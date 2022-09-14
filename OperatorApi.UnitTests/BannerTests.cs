using System;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Banner;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class BannerTests
{
    [Test]
    public async Task User_Can_Get_Banners()
    {
        var fakeBrandRepository = Substitute.For<IBrandRepository>();

        var sut = new GetBannersQueryService(fakeBrandRepository);

        var query = new GetBannersQuery(Guid.NewGuid());

        await sut.QueryAsync(query).CountAsync();

        fakeBrandRepository.Received(1)
            .QueryBrandBannersAsync(Arg.Is(query.BrandId));
    }

    [Test]
    public async Task User_Can_Get_Max_10_Banners()
    {
        var fakeBrandRepository = Substitute.For<IBrandRepository>();

        var sut = new GetBannersQueryService(fakeBrandRepository);

        var query = new GetBannersQuery(Guid.NewGuid());

        fakeBrandRepository.QueryBrandBannersAsync(Arg.Is(query.BrandId))
            .Returns(new[] {
                new BannerInfo(Guid.NewGuid(), "Test1", null),
                new BannerInfo(Guid.NewGuid(), "Test2", null),
                new BannerInfo(Guid.NewGuid(), "Test3", null),
                new BannerInfo(Guid.NewGuid(), "Test4", null),
                new BannerInfo(Guid.NewGuid(), "Test5", null),
                new BannerInfo(Guid.NewGuid(), "Test6", null),
                new BannerInfo(Guid.NewGuid(), "Test7", null),
                new BannerInfo(Guid.NewGuid(), "Test8", null),
                new BannerInfo(Guid.NewGuid(), "Test9", null),
                new BannerInfo(Guid.NewGuid(), "Test10", null),
                new BannerInfo(Guid.NewGuid(), "Test11", null)
            }.ToAsyncEnumerable());

        var actual = await sut.QueryAsync(query).CountAsync();

        Assert.AreEqual(10, actual);
    }
}
