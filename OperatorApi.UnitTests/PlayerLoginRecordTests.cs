#nullable enable

using System;
using NSubstitute;
using NUnit.Framework;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Exceptions;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Player;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class PlayerLoginRecordTests
{
	[Test]
	public void Validate_PlayerLoginRecord_Throw_Exception_When_LoginRecord_Is_Deauthorized()
	{
		var fakeLoginRecordRepository = Substitute.For<IPlayerLoginRecordRepository>();

		var loginRecord = new LoginRecordInfo(
			Guid.NewGuid(),
			"player1",
			LoginStatus.Deauthorized,
			PlatformType.Desktop,
			DateTimeOffset.UtcNow,
			DateTimeOffset.UtcNow.AddHours(12));

		_ = fakeLoginRecordRepository.GetPlayerLoginRecordAsync(Arg.Is(loginRecord.Id))
			.Returns(loginRecord);

		var command = new ValidatePlayerLoginRecordCommand(loginRecord.Id, loginRecord.PlayerCode);

		var sut = new ValidatePlayerLoginRecordCommandService(fakeLoginRecordRepository);

		_ = Assert.ThrowsAsync<PlayerLoginExpiredException>(
			async () => await sut.ExecuteAsync(command).ConfigureAwait(false));
	}

	[Test]
	public void Validate_PlayerLoginRecord_Throw_Exception_When_LoginRecord_Is_Expired()
	{
		var fakeLoginRecordRepository = Substitute.For<IPlayerLoginRecordRepository>();

		var loginRecord = new LoginRecordInfo(
			Guid.NewGuid(),
			"player1",
			LoginStatus.Enabled,
			PlatformType.Desktop,
			DateTimeOffset.UtcNow.AddDays(-2),
			DateTimeOffset.UtcNow.AddDays(-1));

		_ = fakeLoginRecordRepository.GetPlayerLoginRecordAsync(Arg.Is(loginRecord.Id))
			.Returns(loginRecord);

		var command = new ValidatePlayerLoginRecordCommand(loginRecord.Id, loginRecord.PlayerCode);

		var sut = new ValidatePlayerLoginRecordCommandService(fakeLoginRecordRepository);

		_ = Assert.ThrowsAsync<PlayerLoginExpiredException>(
			async () => await sut.ExecuteAsync(command).ConfigureAwait(false));
	}

	[Test]
	public void Validate_PlayerLoginRecord_Throw_Exception_When_LoginRecord_Is_Null()
	{
		var fakeLoginRecordRepository = Substitute.For<IPlayerLoginRecordRepository>();

		var loginRecordId = Guid.NewGuid();
		var playerCode = "player1";

		_ = fakeLoginRecordRepository.GetPlayerLoginRecordAsync(Arg.Is(loginRecordId))
			.Returns((LoginRecordInfo?)null);

		var command = new ValidatePlayerLoginRecordCommand(loginRecordId, playerCode);

		var sut = new ValidatePlayerLoginRecordCommandService(fakeLoginRecordRepository);

		_ = Assert.ThrowsAsync<PlayerLoginExpiredException>(
			async () => await sut.ExecuteAsync(command).ConfigureAwait(false));
	}

	[Test]
	public void Validate_PlayerLoginRecord_Throw_Exception_When_PlayerCode_Is_Not_Match()
	{
		var fakeLoginRecordRepository = Substitute.For<IPlayerLoginRecordRepository>();

		var loginRecord = new LoginRecordInfo(
			Guid.NewGuid(),
			"player1",
			LoginStatus.Enabled,
			PlatformType.Desktop,
			DateTimeOffset.UtcNow,
			DateTimeOffset.UtcNow.AddHours(12));

		_ = fakeLoginRecordRepository.GetPlayerLoginRecordAsync(Arg.Is(loginRecord.Id))
			.Returns(loginRecord);

		var command = new ValidatePlayerLoginRecordCommand(loginRecord.Id, "player2");

		var sut = new ValidatePlayerLoginRecordCommandService(fakeLoginRecordRepository);

		_ = Assert.ThrowsAsync<PlayerLoginExpiredException>(
			  async () => await sut.ExecuteAsync(command).ConfigureAwait(false));
	}

	[Test]
	public void Validate_PlayerLoginRecord_Throw_No_Exception_When_Is_Valid()
	{
		var fakeLoginRecordRepository = Substitute.For<IPlayerLoginRecordRepository>();

		var loginRecord = new LoginRecordInfo(
			Guid.NewGuid(),
			"player1",
			LoginStatus.Enabled,
			PlatformType.Desktop,
			DateTimeOffset.UtcNow,
			DateTimeOffset.UtcNow.AddHours(12));

		_ = fakeLoginRecordRepository.GetPlayerLoginRecordAsync(Arg.Is(loginRecord.Id))
			  .Returns(loginRecord);

		var command = new ValidatePlayerLoginRecordCommand(loginRecord.Id, loginRecord.PlayerCode);

		var sut = new ValidatePlayerLoginRecordCommandService(fakeLoginRecordRepository);

		Assert.DoesNotThrowAsync(async () => await sut.ExecuteAsync(command).ConfigureAwait(false));
	}
}
