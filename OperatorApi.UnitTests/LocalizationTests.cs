using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Localization;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class LocalizationTests
{
    [Test]
    public async Task Get_LocalizationStrings_By_CultureCode()
    {
        var fakeSysRepository = Substitute.For<ISysRepository>();

        var sut = new GetAllLocalizationStringsQueryService(fakeSysRepository);

        var query = new GetAllLocalizationStringsQuery("en-US");

        await sut.GetAsync(query);

        _ = fakeSysRepository.Received(1)
            .QueryAllLocalizationStringsAsync(Arg.Is(query.CultureCode));
    }
}
