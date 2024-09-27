using System.Collections.Immutable;
using System.Globalization;
using ExpectedObjects;
using NUnit.Framework.Legacy;
using Tgs.BrandCertificate.Clients;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Core.Infrastructures.Mail;
using TGS.Core.Infrastructures.Redis;
using TGS.Core.Models.Enums;
using TGS.Services.Operations;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Player;
using TGS.Services.Operator.ValidateCode;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class PlayerEmailTests
{
	[Test]
	public async Task Commit_Init_Player_Email_If_Brand_Has_BankInfoRegisterFlow_Should_Set_Email_Only()
	{
		var fakeRedisCacheManager = Substitute.For<IRedisCacheManager>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeValidateCodeOperator = Substitute.For<IValidateCodeOperator>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var playerCode = "testPlayerCode";
		var validateCode = "123456";
		var securityKey = "3F8E5681-50C8-4F2F-8379-3AA6EF2C6064";
		var email = "test@mail.com";

		var brandInfo = new Tgs.BrandCertificate.Clients.BrandInfo
		{
			Id = Guid.NewGuid(),
			Code = "code",
			Currency = "CNY",
			BankInfoRegisterFlowEnabled = true,
			EmailRegisterFlowEnabled = true
		};

		_ = fakeValidateCodeOperator.GetValidateCodeDataAsync<EmailValidateData>(
				Arg.Is(validateCode),
				Arg.Is(securityKey))
			.Returns(new EmailValidateData(email, playerCode));

		var command = new CommitInitPlayerEmailCommand(
			securityKey,
			validateCode,
			brandInfo.Id);

		var sut = new CommitInitPlayerEmailCommandService(
			fakePlayerServiceClient,
			fakeValidateCodeOperator,
			fakeRedisCacheManager);

		await sut.ExecuteAsync(command).ConfigureAwait(false);

		_ = fakePlayerServiceClient.Received(1)
			.SetPlayerPropertiesAsync(
				Arg.Is(playerCode),
				Arg.Is<IImmutableDictionary<PlayerPropertyName, string>>(
					dict => dict[PlayerPropertyName.Email] == email));

		_ = fakeRedisCacheManager.Received(1)
			.ClearCacheAsync(
				Arg.Is<object>(x => x.ToString() == nameof(RedisCacheType.Player)),
				Arg.Is<string>(x => x == playerCode),
				Arg.Any<bool>());
	}

	[Test]
	public void Commit_Init_Player_Email_If_Brand_Has_Same_Email_Should_Throw_Exception()
	{
		var fakeRedisCacheManager = Substitute.For<IRedisCacheManager>();
		var fakeValidateCodeOperator = Substitute.For<IValidateCodeOperator>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var playerCode = "testPlayerCode";
		var validateCode = "123456";
		var securityKey = "3F8E5681-50C8-4F2F-8379-3AA6EF2C6064";
		var email = "test@mail.com";

		var brandInfo = new Tgs.BrandCertificate.Clients.BrandInfo
		{
			Id = Guid.NewGuid(),
			Code = "code",
			Currency = "CNY",
			EmailRegisterFlowEnabled = true
		};

		_ = fakeValidateCodeOperator.GetValidateCodeDataAsync<EmailValidateData>(
			Arg.Is(validateCode),
			Arg.Is(securityKey))
			.Returns(new EmailValidateData(email, playerCode));

		_ = fakePlayerServiceClient.GetPlayerInfoByEmailAsync(
			Arg.Is(brandInfo.Id),
			Arg.Is(email))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				PlayerCode = "other"
			});

		var command = new CommitInitPlayerEmailCommand(
			securityKey,
			validateCode,
			brandInfo.Id);

		var sut = new CommitInitPlayerEmailCommandService(
			fakePlayerServiceClient,
			fakeValidateCodeOperator,
			fakeRedisCacheManager);

		_ = Assert.ThrowsAsync<BrandEmailDuplicateException>(async () => await sut.ExecuteAsync(command));
	}

	[Test]
	public async Task Commit_Init_Player_Email_If_Brand_Use_EmailRegisterFlow_Should_Success()
	{
		var fakeRedisCacheManager = Substitute.For<IRedisCacheManager>();
		var fakeValidateCodeOperator = Substitute.For<IValidateCodeOperator>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var playerCode = "testPlayerCode";
		var validateCode = "123456";
		var securityKey = "3F8E5681-50C8-4F2F-8379-3AA6EF2C6064";
		var email = "test@mail.com";

		var brandInfo = new Tgs.BrandCertificate.Clients.BrandInfo
		{
			Id = Guid.NewGuid(),
			Code = "code",
			Currency = "CNY",
			EmailRegisterFlowEnabled = true
		};

		_ = fakeValidateCodeOperator.GetValidateCodeDataAsync<EmailValidateData>(
				Arg.Is(validateCode),
				Arg.Is(securityKey))
			.Returns(new EmailValidateData(email, playerCode));

		var command = new CommitInitPlayerEmailCommand(
			securityKey,
			validateCode,
			brandInfo.Id);

		var sut = new CommitInitPlayerEmailCommandService(
			fakePlayerServiceClient,
			fakeValidateCodeOperator,
			fakeRedisCacheManager);

		await sut.ExecuteAsync(command).ConfigureAwait(false);

		_ = fakePlayerServiceClient.Received(1)
			.SetPlayerPropertiesAsync(
				Arg.Is(playerCode),
				Arg.Is<IImmutableDictionary<PlayerPropertyName, string>>(
					dict => dict[PlayerPropertyName.Email] == email));

		_ = fakeRedisCacheManager.Received(1)
			.ClearCacheAsync(
				Arg.Is<object>(x => x.ToString() == nameof(RedisCacheType.Player)),
				Arg.Is<string>(x => x == playerCode),
				Arg.Any<bool>());
	}

	[Test]
	public void Commit_Init_Player_Email_If_Not_Verify_ValidateCode_Should_Throw_Exception()
	{
		var fakeRedisCacheManager = Substitute.For<IRedisCacheManager>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeValidateCodeOperator = Substitute.For<IValidateCodeOperator>();

		var validateCode = "123456";
		var securityKey = "3F8E5681-50C8-4F2F-8379-3AA6EF2C6064";

		var brandInfo = new Tgs.BrandCertificate.Clients.BrandInfo
		{
			Id = Guid.NewGuid(),
			Code = "code",
			Currency = "CNY",
			EmailRegisterFlowEnabled = true
		};

		_ = fakeValidateCodeOperator
			.GetValidateCodeDataAsync<EmailValidateData>(
				Arg.Is(validateCode),
				Arg.Is(securityKey))
			.Returns((EmailValidateData)null);

		var command = new CommitInitPlayerEmailCommand(
			securityKey,
			validateCode,
			brandInfo.Id);

		var sut = new CommitInitPlayerEmailCommandService(
			fakePlayerServiceClient,
			fakeValidateCodeOperator,
			fakeRedisCacheManager);

		_ = Assert.ThrowsAsync<ValidateCodeNotVerifyException>(async () => await sut.ExecuteAsync(command));
	}

	[Test]
	public void Commit_Set_Player_Email_IF_Not_Verify_ValidateCode_Should_Throw_Exception()
	{
		var fakeValidateCodeOperator = Substitute.For<IValidateCodeOperator>();
		var fakePlayerOperations = Substitute.For<IPlayerOperations>();
		var fakeCheckService = Substitute.For<IGetService<CheckBrandEmailQuery, bool>>();

		var sut = new CommitSetPlayerEmailCommandService(
			fakeValidateCodeOperator,
			fakePlayerOperations,
			fakeCheckService);

		var playerCode = "testPlayerCode";
		var validateCode = "123456";
		var token = "3F8E5681-50C8-4F2F-8379-3AA6EF2C6064";
		var brandId = Guid.NewGuid();

		var command = new CommitSetPlayerEmailCommand(playerCode, token, validateCode, brandId);

		_ = fakeValidateCodeOperator
			.GetValidateCodeDataAsync<EmailValidateData>(Arg.Any<string>(), Arg.Any<string>())
			.Returns((EmailValidateData)null);

		_ = Assert.ThrowsAsync<ValidateCodeNotVerifyException>(async () => await sut.ExecuteAsync(command));
	}

	[Test]
	public async Task Commit_Set_Player_Email_Should_Success()
	{
		var fakeValidateCodeOperator = Substitute.For<IValidateCodeOperator>();
		var fakePlayerOperations = Substitute.For<IPlayerOperations>();
		var fakeCheckService = Substitute.For<IGetService<CheckBrandEmailQuery, bool>>();

		var sut = new CommitSetPlayerEmailCommandService(
			fakeValidateCodeOperator,
			fakePlayerOperations,
			fakeCheckService);

		var playerCode = "testPlayerCode";
		var validateCode = "123456";
		var securityKey = "3F8E5681-50C8-4F2F-8379-3AA6EF2C6064";
		var email = "test@raypro.com";
		var brandId = Guid.NewGuid();

		var command = new CommitSetPlayerEmailCommand(playerCode, securityKey, validateCode, brandId);

		_ = fakeValidateCodeOperator
			.GetValidateCodeDataAsync<EmailValidateData>(Arg.Is(validateCode), Arg.Is(securityKey))
			.Returns(new EmailValidateData(email, playerCode));

		await sut.ExecuteAsync(command);

		_ = fakePlayerOperations
			.Received(1)
			.SetEmailAsync(Arg.Is(playerCode), Arg.Is(email), Arg.Any<DateTimeOffset>());
	}

	[Test]
	public async Task Same_Brand_And_Same_Email_But_Same_Player_Should_Get_False()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new CheckBrandEmailQueryService(fakePlayerServiceClient);

		var query = new CheckBrandEmailQuery(
			new Guid("4e86719f-243f-46f6-b010-5c758b7285af"),
			"TEST@mail.com",
			"playerCode");

		_ = fakePlayerServiceClient
			.GetPlayerInfoByEmailAsync(
				Arg.Is(query.BrandId),
				Arg.Is(query.Email))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				BrandId = query.BrandId,
				PlayerCode = query.PlayerCode
			});

		var actual = await sut.GetAsync(query);

		ClassicAssert.IsFalse(actual);
	}

	[Test]
	public async Task Same_Brand_And_Same_Email_Should_Get_True()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new CheckBrandEmailQueryService(fakePlayerServiceClient);

		var query = new CheckBrandEmailQuery(
			new Guid("4e86719f-243f-46f6-b010-5c758b7285af"),
			"TEST@mail.com",
			"playerCode1");

		_ = fakePlayerServiceClient
			.GetPlayerInfoByEmailAsync(
				Arg.Is(query.BrandId),
				Arg.Is(query.Email))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				BrandId = query.BrandId,
				PlayerCode = "playerCode"
			});

		var actual = await sut.GetAsync(query);

		ClassicAssert.IsTrue(actual);
	}

	[Test]
	public void Set_Same_Email_In_Same_Brand_Should_Throw_Exception()
	{
		var fakeValidateCodeOperator = Substitute.For<IValidateCodeOperator>();
		var fakePlayerOperations = Substitute.For<IPlayerOperations>();
		var fakeCheckEmailService = Substitute.For<IGetService<CheckBrandEmailQuery, bool>>();

		var sut = new CommitSetPlayerEmailCommandService(
			fakeValidateCodeOperator,
			fakePlayerOperations,
			fakeCheckEmailService);
		var brandId = Guid.NewGuid();

		_ = fakeValidateCodeOperator
			.GetValidateCodeDataAsync<EmailValidateData>(Arg.Any<string>(), Arg.Any<string>())
			.Returns(new EmailValidateData("test@email.com", "playerCode"));

		_ = fakeCheckEmailService
			.GetAsync(Arg.Any<CheckBrandEmailQuery>())
			.Returns(true);

		_ = Assert.ThrowsAsync<BrandEmailDuplicateException>(async ()
			=> await sut.ExecuteAsync(new CommitSetPlayerEmailCommand(
				"playerCode",
				"a0e4ec77-d05b-41c4-b7dd-dde8e46c27bc",
				"909542",
				brandId)));
	}

	[Test]
	public async Task Start_Set_Player_Email_Should_Not_Send_Confirm_Mail_If_Is_Register_Flow()
	{
		var fakeEmailClient = Substitute.For<IEmailClient>();
		var fakeTelegramClient = Substitute.For<ITelegramClient>();
		var fakeValidateCodeGenerator = Substitute.For<IValidateCodeGenerator>();
		var fakeValidateCodeOperator = Substitute.For<IValidateCodeOperator>();

		var sut = new StartSetPlayerEmailCommandService(
			fakeEmailClient,
			fakeTelegramClient,
			fakeValidateCodeGenerator,
			fakeValidateCodeOperator);

		var playerCode = "testPlayerCode";
		var email = "test@raypro.com";
		var brand = "GPI";
		var username = "test123";
		var command = new StartSetPlayerEmailCommand(
			playerCode,
			email,
			brand,
			username,
			CultureInfo.GetCultureInfo("en-US"),
			true);

		var validateCode = "123456";
		var expectedSecurityKey = "3F8E5681-50C8-4F2F-8379-3AA6EF2C6064";

		_ = fakeValidateCodeGenerator.GenerateSecurityKey().Returns(expectedSecurityKey);

		_ = fakeValidateCodeOperator
			.SaveValidateDataAsync(Arg.Is(validateCode), Arg.Is(expectedSecurityKey), Arg.Any<EmailValidateData>())
			.Returns(true);

		var actualSecurityKey = await sut.GetAsync(command);

		_ = fakeEmailClient
			.Received(0)
			.SendAsync(Arg.Any<SendEmailRequest>());

		_ = fakeValidateCodeGenerator
			.Received(0).GenerateCode();

		ClassicAssert.AreEqual(expectedSecurityKey, actualSecurityKey);
	}

	[Test]
	public async Task Start_Set_Player_Email_Should_Send_Confirm_Mail_If_Not_Register_Flow()
	{
		var fakeEmailClient = Substitute.For<IEmailClient>();
		var fakeTelegramClient = Substitute.For<ITelegramClient>();
		var fakeValidateCodeGenerator = Substitute.For<IValidateCodeGenerator>();
		var fakeValidateCodeOperator = Substitute.For<IValidateCodeOperator>();

		var sut = new StartSetPlayerEmailCommandService(
			fakeEmailClient,
			fakeTelegramClient,
			fakeValidateCodeGenerator,
			fakeValidateCodeOperator);

		var playerCode = "testPlayerCode";
		var email = "test@raypro.com";
		var brand = "GPI";
		var username = "test123";
		var command = new StartSetPlayerEmailCommand(playerCode, email, brand, username,CultureInfo.GetCultureInfo("en-US"), false);

		var validateCode = "123456";
		var expectedSecurityKey = "3F8E5681-50C8-4F2F-8379-3AA6EF2C6064";

		_ = fakeValidateCodeGenerator.GenerateCode().Returns(validateCode);
		_ = fakeValidateCodeGenerator.GenerateSecurityKey().Returns(expectedSecurityKey);

		_ = fakeValidateCodeOperator
			.SaveValidateDataAsync(Arg.Is(validateCode), Arg.Is(expectedSecurityKey), Arg.Any<EmailValidateData>())
			.Returns(true);

		var expected = new
		{
			Receivers = new[] { email },
			Subject = "Verification Code",
			Content = $"Verification code is {validateCode}, it will expire after 10 minutes."
		}.ToExpectedObject();

		SendEmailRequest actual = null;
		_ = fakeEmailClient
			.SendAsync(Arg.Do<SendEmailRequest>(msg => actual = msg))
			.Returns(ValueTask.FromResult(new SendEmailResponse()));
		var actualSecurityKey = await sut.GetAsync(command);

		_ = fakeEmailClient
			.Received(1)
			.SendAsync(Arg.Any<SendEmailRequest>());

		expected.ShouldMatch(actual);
		ClassicAssert.AreEqual(expectedSecurityKey, actualSecurityKey);
	}
}