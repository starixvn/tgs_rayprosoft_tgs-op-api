#nullable enable

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using ExpectedObjects;
using NSubstitute;
using NUnit.Framework;
using Tgs.Core.Infrastructures.Security;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Tokens;
using TGS.Services.Operator.ApplicationServices.Player;

namespace TGS.Tests.OperatorApi.UnitTests;

internal class PlayerTokenParserTests
{
	[Test]
	public async Task Can_Parse_PlayerToken()
	{
		var fakeTokenHandler = Substitute.For<IJwtTokenHandler>();

		var playerCode = "player1";
		var platformType = PlatformType.Desktop;
		var expiredOn = DateTimeOffset.UtcNow;
		var loginRecordId = Guid.NewGuid();

		var claims = new[]
		{
			new Claim(JwtRegisteredClaimNames.Sub, playerCode),
			new Claim("login_id", loginRecordId.ToString()),
			new Claim("platform_type", platformType.ToString())
		};

		var jwtSecurityToken = new JwtSecurityToken(claims: claims, expires: expiredOn.UtcDateTime);

		var command = new PlayerTokenParserCommand("PlayerToken");

		var sut = new PlayerTokenParserCommandService(fakeTokenHandler);

		_ = fakeTokenHandler.ReadToken(
				Arg.Is(command.PlayerToken))
			.Returns(jwtSecurityToken);

		var expected = new PlayerTokenData
		{
			PlayerCode = playerCode,
			PlatformType = platformType,
			LoginRecordId = loginRecordId,
			ExpiredOn = expiredOn
		};

		var actual = await sut.GetAsync(command);
		
		Assert.NotNull(actual);

		expected.ToExpectedObject(ctx 
			=> ctx.Ignore(x => x.ExpiredOn)
				.IgnoreEqualsOverride())
			.ShouldEqual(actual);
	}

	[Test]
	public async Task Fail_To_Parse_Token_When_Token_Is_Empty()
	{
		var fakeTokenHandler = Substitute.For<IJwtTokenHandler>();
		
		var command = new PlayerTokenParserCommand(string.Empty);

		var sut = new PlayerTokenParserCommandService(fakeTokenHandler);

		var actual = await sut.GetAsync(command);

		Assert.Null(actual);
	}

	[Test]
	public async Task Fail_To_Parse_Token_When_Missing_PlayerCode_Claim()
	{
		var fakeTokenHandler = Substitute.For<IJwtTokenHandler>();

		var platformType = PlatformType.Desktop;
		var expiredOn = DateTimeOffset.UtcNow;
		var loginRecordId = Guid.NewGuid();

		var claims = new[]
		{
			new Claim("login_id", loginRecordId.ToString()),
			new Claim("platform_type", platformType.ToString())
		};

		var jwtSecurityToken = new JwtSecurityToken(claims: claims, expires: expiredOn.UtcDateTime);

		var command = new PlayerTokenParserCommand("PlayerToken");

		var sut = new PlayerTokenParserCommandService(fakeTokenHandler);

		_ = fakeTokenHandler.ReadToken(
				Arg.Is(command.PlayerToken))
			.Returns(jwtSecurityToken);
		
		var actual = await sut.GetAsync(command);

		Assert.Null(actual);
	}

	[Test]
	public async Task Fail_To_Parse_Token_When_Missing_LoginId_Claim()
	{
		var fakeTokenHandler = Substitute.For<IJwtTokenHandler>();

		var playerCode = "player1";
		var platformType = PlatformType.Desktop;
		var expiredOn = DateTimeOffset.UtcNow;

		var claims = new[]
		{           
			new Claim(JwtRegisteredClaimNames.Sub, playerCode),
			new Claim("platform_type", platformType.ToString())
		};

		var jwtSecurityToken = new JwtSecurityToken(claims: claims, expires: expiredOn.UtcDateTime);

		var command = new PlayerTokenParserCommand("PlayerToken");

		var sut = new PlayerTokenParserCommandService(fakeTokenHandler);

		_ = fakeTokenHandler.ReadToken(
				Arg.Is(command.PlayerToken))
			.Returns(jwtSecurityToken);

		var actual = await sut.GetAsync(command);

		Assert.Null(actual);
	}

	[Test]
	public async Task Fail_To_Parse_Token_When_Missing_PlatformType_Claim()
	{
		var fakeTokenHandler = Substitute.For<IJwtTokenHandler>();

		var playerCode = "player1";
		var expiredOn = DateTimeOffset.UtcNow;
		var loginRecordId = Guid.NewGuid();

		var claims = new[]
		{
			new Claim(JwtRegisteredClaimNames.Sub, playerCode),
			new Claim("login_id", loginRecordId.ToString())
		};

		var jwtSecurityToken = new JwtSecurityToken(claims: claims, expires: expiredOn.UtcDateTime);

		var command = new PlayerTokenParserCommand("PlayerToken");

		var sut = new PlayerTokenParserCommandService(fakeTokenHandler);

		_ = fakeTokenHandler.ReadToken(
				Arg.Is(command.PlayerToken))
			.Returns(jwtSecurityToken);

		var actual = await sut.GetAsync(command);

		Assert.Null(actual);
	}

	[Test]
	public async Task Fail_To_Parse_Token_When_Expire_Claim()
	{
		var fakeTokenHandler = Substitute.For<IJwtTokenHandler>();

		var playerCode = "player1";
		var platformType = PlatformType.Desktop;
		var loginRecordId = Guid.NewGuid();

		var claims = new[]
		{
			new Claim(JwtRegisteredClaimNames.Sub, playerCode),
			new Claim("login_id", loginRecordId.ToString()),
			new Claim("platform_type", platformType.ToString())
		};

		var jwtSecurityToken = new JwtSecurityToken(claims: claims);

		var command = new PlayerTokenParserCommand("PlayerToken");

		var sut = new PlayerTokenParserCommandService(fakeTokenHandler);

		_ = fakeTokenHandler.ReadToken(
				Arg.Is(command.PlayerToken))
			.Returns(jwtSecurityToken);

		var actual = await sut.GetAsync(command);

		Assert.Null(actual);
	}
}
