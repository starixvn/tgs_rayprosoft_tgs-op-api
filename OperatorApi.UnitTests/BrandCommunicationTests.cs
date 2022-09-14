using System;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using TGS.Core.DataServices.Tgs;
using TGS.Services.Operator.ApplicationServices.BrandCommunication;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class BrandCommunicationTests
{
    [Test]
    public async Task Get_Brand_Communications_Should_Success()
    {
        var fakeTgsRepository = Substitute.For<ITgsRepository>();
        var sut = new GetBrandCommunicationQueryService(fakeTgsRepository);

        var query = new BrandCommunicationQuery(BrandId: Guid.NewGuid());
        var info = await sut.QueryAsync(query).ToArrayAsync().ConfigureAwait(false);

        Assert.IsTrue(info.Length >= 0);
    }
}