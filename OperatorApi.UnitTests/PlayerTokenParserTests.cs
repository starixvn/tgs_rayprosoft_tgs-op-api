#nullable enable

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ExpectedObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework.Legacy;
using Tgs.Core.Infrastructures.Security;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Tokens;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Player;

namespace TGS.Tests.OperatorApi.UnitTests;

internal class PlayerTokenParserTests
{
	[Test]
	public async Task Can_Parse_PlayerToken()
	{
		var fakeTokenHandler = Substitute.For<IJwtTokenHandler>();

		var brandId = Guid.NewGuid();
		var playerCode = "player1";
		var platformType = PlatformType.Desktop;
		var expiredOn = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
		var loginRecordId = Guid.NewGuid();

		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, playerCode),
			new Claim("login_id", loginRecordId.ToString()),
			new Claim("platform_type", platformType.ToString()),
			new Claim(JwtRegisteredClaimNames.Exp, expiredOn.ToUnixTimeSeconds().ToString())
		};

		var jwtSecurityToken = new JwtSecurityToken(claims: claims, expires: expiredOn.UtcDateTime);
		var fakeBrandRepository = Substitute.For<IBrandRepository>();

		var sut = new PlayerTokenParserCommandService(
			fakeTokenHandler,
			fakeBrandRepository,
			NullLogger<PlayerTokenParserCommandService>.Instance);

		var command = new PlayerTokenParserCommand(brandId, "PlayerToken");

		_ = fakeBrandRepository.GetSecretKeyAsync(Arg.Is(command.BrandId))
			.Returns("testkey");

		_ = fakeTokenHandler.ValidateToken(
			Arg.Is(command.PlayerToken),
			Arg.Any<TokenValidationParameters>(),
			Arg.Any<Action<Exception>>(),
			Arg.Any<Action<SecurityTokenExpiredException>>())
			.Returns((true, new ClaimsPrincipal(new ClaimsIdentity(claims)), new JwtSecurityToken()));

		var expected = new PlayerTokenData
		{
			PlayerCode = playerCode,
			PlatformType = platformType,
			LoginRecordId = loginRecordId,
			ExpiredOn = expiredOn
		};

		var actual = await sut.GetAsync(command);

		ClassicAssert.NotNull(actual);

		expected.ToExpectedObject(ctx
			=> ctx.Ignore(x => x.ExpiredOn)
				.IgnoreEqualsOverride())
			.ShouldEqual(actual);
	}
}