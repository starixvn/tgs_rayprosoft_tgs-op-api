using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Tgs.Core.Infrastructures.Security;
using TGS.Services.Operations;
using TGS.Services.Operations.Validators;
using TGS.Services.Operator.ApplicationServices.Auth;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class AuthTests
{
    [Test]
    public async Task Can_Get_Identity_By_ApiKey()
    {
        var fakeBrandOperations = Substitute.For<IBrandOperations>();

        var sut = new GetAuthQueryService(
            fakeBrandOperations);

        var apiKey = "I7nKiimrZQOqnJ6ryjUw74c0Hi64KC6Myunhob4cHh8VOUIuZh4p63czefxyyzbZ";
        var currency = "CNY";

        var query = new GetAuthQuery(apiKey, currency);

        var actual = await sut.GetAsync(query);

        _ = fakeBrandOperations.Received(1)
            .GetBrandIdByApiKeyAsync(Arg.Is(apiKey));

        Assert.IsNotNull(actual);
    }

    [Test]
    public async Task Can_Make_Token()
    {
        var fakeBrandOperations = Substitute.For<IBrandOperations>();
        var fakeJwtTokenHandler = Substitute.For<IJwtTokenHandler>();

        var sut = new MakeBrandAuthTokenCommandService(fakeJwtTokenHandler, fakeBrandOperations);

        var brandId = Guid.NewGuid();
        var currency = "CNY";

        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, brandId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("curr", currency)
            });

        var command = new MakeBrandAuthTokenCommand(brandId, identity, "operator-api");

        var actual = await sut.GetAsync(command);

        Assert.IsNotNull(actual);
    }
}