using System.Collections.Immutable;
using System.Globalization;
using Moq;
using NSubstitute;
using NUnit.Framework.Legacy;
using Tgs.BrandCertificate.Clients;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Core.Infrastructures.Redis;
using TGS.Core.Models.Enums;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Player;
using TGS.Services.Operator.ValidateCode;
using TGS.SmsService.Client;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class PlayerPhoneTests
{
	[Test]
	public async Task CheckPhone_If_Not_Input_PlayerCode()
	{
		var brandId = Guid.Parse("1D46DDD7-EB42-43A0-CB2F-724E59622CA5");
		var areaCode = "+852";
		var phone = "66666666";

		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new CheckBrandPhoneCanUseQueryService(fakePlayerServiceClient);

		var query = new CheckBrandPhoneCanUseQuery(brandId, null, areaCode, phone);

		_ = fakePlayerServiceClient.GetPlayerInfoByPhoneAsync(
			Arg.Is(brandId),
			Arg.Is(areaCode),
			Arg.Is(phone))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				PlayerCode = "playerCode"
			});

		var actual = await sut.GetAsync(query);

		ClassicAssert.IsTrue(actual);
	}

	[Test]
	public async Task Commit_Init_Player_Phone_If_Brand_Use_SmsRegisterFlow_Should_Success()
	{
		var moqValidateCodeOperator = new Mock<IValidateCodeOperator>();
		var moqCacheManager = new Mock<IRedisCacheManager>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new CommitInitPlayerPhoneCommandService(
			moqValidateCodeOperator.Object,
			moqCacheManager.Object,
			fakePlayerServiceClient);

		var playerCode = "testPlayerCode";
		var validateCode = "123456";
		var securityKey = "3F8E5681-50C8-4F2F-8379-3AA6EF2C6064";
		var phone = "0988123456";
		var areaCode = "886";

		var brandInfo = new Tgs.BrandCertificate.Clients.BrandInfo
		{
			Id = Guid.NewGuid(),
			Code = "code",
			Currency = "CNY",
			SmsRegisterFlowEnabled = true
		};

		var command = new CommitInitPlayerPhoneCommand(
			securityKey,
			validateCode,
			"127.0.0.1",
			"localhost",
			brandInfo.Id);

		_ = moqValidateCodeOperator
			.Setup(
				fake => fake.GetValidateCodeDataAsync<PhoneValidateData>(
					It.Is<string>(x => x == validateCode),
					It.Is<string>(x => x == securityKey)))
			.Returns(ValueTask.FromResult(new PhoneValidateData(
				areaCode,
				phone,
				playerCode)));

		await sut.ExecuteAsync(command);

		_ = fakePlayerServiceClient.Received(1)
			.SetPlayerPropertiesAsync(
				Arg.Is(playerCode),
				Arg.Is<ImmutableDictionary<PlayerPropertyName, string>>(
					dict => dict.Count == 3
						&& dict[PlayerPropertyName.AreaCode] == areaCode
						&& dict[PlayerPropertyName.Phone] == phone
						&& dict[PlayerPropertyName.PhoneAuthentication] == true.ToString()));

		moqCacheManager.Verify(
			fake => fake.ClearCacheAsync(
				It.Is<object>(x => x.ToString() == nameof(RedisCacheType.Player)),
				It.Is<object>(x => x.ToString() == playerCode),
				It.Is<bool>(x => x == false)),
			Times.Exactly(1));
	}

	[Test]
	public void Commit_Init_Player_Phone_If_Not_Verify_ValidateCode_Should_Throw_Exception()
	{
		var moqValidateCodeOperator = new Mock<IValidateCodeOperator>();
		var moqCacheManager = new Mock<IRedisCacheManager>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new CommitInitPlayerPhoneCommandService(
			moqValidateCodeOperator.Object,
			moqCacheManager.Object,
			fakePlayerServiceClient);

		var validateCode = "123456";
		var securityKey = "3F8E5681-50C8-4F2F-8379-3AA6EF2C6064";

		var brandInfo = new Tgs.BrandCertificate.Clients.BrandInfo
		{
			Id = Guid.NewGuid(),
			Code = "code",
			Currency = "CNY",
			SmsRegisterFlowEnabled = true
		};

		var command = new CommitInitPlayerPhoneCommand(
			securityKey,
			validateCode,
			"127.0.0.1",
			"localhost",
			brandInfo.Id);

		_ = moqValidateCodeOperator
			.Setup(
				fake => fake.GetValidateCodeDataAsync<PhoneValidateData>(
					It.IsAny<string>(),
					It.IsAny<string>()))
			.Returns(ValueTask.FromResult<PhoneValidateData>(null));

		_ = Assert.ThrowsAsync<ValidateCodeNotVerifyException>(async () => await sut.ExecuteAsync(command));
	}

	[Test]
	public void Commit_Set_Player_Phone_If_Brand_Has_Same_Phone_Should_Throw_Exception()
	{
		var fakeValidateCodeOperator = Substitute.For<IValidateCodeOperator>();
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakeCacheManager = Substitute.For<IRedisCacheManager>();
		var fakeCheckService = Substitute.For<IGetService<CheckBrandPhoneCanUseQuery, bool>>();

		var sut = new CommitSetPlayerPhoneCommandService(
			fakeValidateCodeOperator,
			fakePlayerRepository,
			fakeCacheManager,
			fakeCheckService);

		var playerCode = "testPlayerCode";
		var validateCode = "123456";
		var securityKey = "3F8E5681-50C8-4F2F-8379-3AA6EF2C6064";
		var brandId = Guid.Parse("4e86719f-243f-46f6-b010-5c758b7285af");
		var areaCode = "852";
		var phone = "67329548";

		var command = new CommitSetPlayerPhoneCommand(brandId, playerCode, securityKey, validateCode);

		_ = fakeValidateCodeOperator
			.GetValidateCodeDataAsync<PhoneValidateData>(validateCode, Arg.Any<string>())
			.Returns(new PhoneValidateData(areaCode, phone, playerCode));

		_ = fakeCheckService
			.GetAsync(new CheckBrandPhoneCanUseQuery(brandId, playerCode, areaCode, phone))
			.Returns(false);

		_ = Assert.ThrowsAsync<BrandPhoneDuplicateException>(async () => await sut.ExecuteAsync(command));
	}

	[Test]
	public void Commit_Set_Player_Phone_If_Not_Verify_ValidateCode_Should_Throw_Exception()
	{
		var moqValidateCodeOperator = new Mock<IValidateCodeOperator>();
		var moqPlayerRepository = new Mock<IPlayerRepository>();
		var moqCacheManager = new Mock<IRedisCacheManager>();
		var moqCheckService = new Mock<IGetService<CheckBrandPhoneCanUseQuery, bool>>();

		var sut = new CommitSetPlayerPhoneCommandService(
			moqValidateCodeOperator.Object,
			moqPlayerRepository.Object,
			moqCacheManager.Object,
			moqCheckService.Object);

		var playerCode = "testPlayerCode";
		var validateCode = "123456";
		var securityKey = "3F8E5681-50C8-4F2F-8379-3AA6EF2C6064";

		var command = new CommitSetPlayerPhoneCommand(Guid.NewGuid(), playerCode, securityKey, validateCode);

		_ = moqValidateCodeOperator
			.Setup(
				fake => fake.GetValidateCodeDataAsync<PhoneValidateData>(
					It.IsAny<string>(),
					It.IsAny<string>()))
			.Returns(ValueTask.FromResult<PhoneValidateData>(null));

		_ = Assert.ThrowsAsync<ValidateCodeNotVerifyException>(async () => await sut.ExecuteAsync(command));
	}

	[Test]
	public async Task Commit_Set_Player_Phone_Should_Success()
	{
		var moqValidateCodeOperator = new Mock<IValidateCodeOperator>();
		var moqPlayerRepository = new Mock<IPlayerRepository>();
		var moqCacheManager = new Mock<IRedisCacheManager>();
		var fakeCheckService = Substitute.For<IGetService<CheckBrandPhoneCanUseQuery, bool>>();

		var sut = new CommitSetPlayerPhoneCommandService(
			moqValidateCodeOperator.Object,
			moqPlayerRepository.Object,
			moqCacheManager.Object,
			fakeCheckService);

		var playerCode = "testPlayerCode";
		var validateCode = "123456";
		var securityKey = "3F8E5681-50C8-4F2F-8379-3AA6EF2C6064";
		var phone = "0988123456";
		var areaCode = "886";

		var command = new CommitSetPlayerPhoneCommand(Guid.NewGuid(), playerCode, securityKey, validateCode);

		_ = fakeCheckService.GetAsync(Arg.Any<CheckBrandPhoneCanUseQuery>())
			.Returns(true);

		_ = moqValidateCodeOperator
			.Setup(
				fake => fake.GetValidateCodeDataAsync<PhoneValidateData>(
					It.Is<string>(x => x == validateCode),
					It.Is<string>(x => x == securityKey)))
			.Returns(ValueTask.FromResult(new PhoneValidateData(
				areaCode,
				phone,
				playerCode)));

		await sut.ExecuteAsync(command);

		moqPlayerRepository.Verify(
			fake => fake.SetPlayerPhoneAsync(
				It.Is<string>(x => x == playerCode),
				It.Is<string>(x => x == areaCode),
				It.Is<string>(x => x == phone)),
			Times.Exactly(1));
	}

	[Test]
	public async Task Same_Brand_And_Other_Player_Should_Get_False()
	{
		var brandId = Guid.Parse("1D46DDD7-EB42-43A0-CB2F-724E59622CA5");
		var playerCode = "playerCode";
		var areaCode = "+852";
		var phone = "66666666";

		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new CheckBrandPhoneCanUseQueryService(fakePlayerServiceClient);

		var query = new CheckBrandPhoneCanUseQuery(brandId, playerCode, areaCode, phone);

		_ = fakePlayerServiceClient.GetPlayerInfoByPhoneAsync(
			Arg.Is(brandId),
			Arg.Is(areaCode),
			Arg.Is(phone))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				PlayerCode = "otherCode"
			});

		var actual = await sut.GetAsync(query);

		ClassicAssert.IsFalse(actual);
	}

	[Test]
	public async Task Same_Brand_And_Same_Phone_And_Same_AreaCode_And_Same_Player_Should_Get_True()
	{
		var brandId = Guid.Parse("1D46DDD7-EB42-43A0-CB2F-724E59622CA5");
		var playerCode = "playerCode";
		var areaCode = "+852";
		var phone = "66666666";

		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new CheckBrandPhoneCanUseQueryService(fakePlayerServiceClient);

		var query = new CheckBrandPhoneCanUseQuery(brandId, playerCode, areaCode, phone);

		_ = fakePlayerServiceClient.GetPlayerInfoByPhoneAsync(
			Arg.Is(brandId),
			Arg.Is(areaCode),
			Arg.Is(phone))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				PlayerCode = playerCode
			});

		var actual = await sut.GetAsync(query);

		ClassicAssert.IsTrue(actual);
	}

	[Test]
	public async Task Start_Set_Player_Phone_Should_Not_Send_Confirm_Message_When_Sms_Disabled()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var moqValidateCodeGenerator = new Mock<IValidateCodeGenerator>();
		var moqValidateCodeOperator = new Mock<IValidateCodeOperator>();
		var moqSmsServiceClient = new Mock<ISmsServiceClient>();

		var sut = new StartSetPlayerPhoneCommandService(
			fakePlayerServiceClient,
			fakeBrandServiceClient,
			moqValidateCodeGenerator.Object,
			moqValidateCodeOperator.Object,
			moqSmsServiceClient.Object);

		var areaCode = "886";
		var phone = "0988123456";
		var player = new Tgs.PlayerService.Client.PlayerInfo
		{
			PlayerCode = "abcdef123456",
			BrandId = Guid.NewGuid()
		};

		var command = new StartSetPlayerPhoneCommand(
			player.PlayerCode,
			areaCode,
			phone,
			CultureInfo.GetCultureInfo("en-US"),
			false);

		var validateCode = "123456";
		var expectedSecurityKey = "3F8E5681-50C8-4F2F-8379-3AA6EF2C6064";

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(player.PlayerCode))
			.Returns(player);

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(player.BrandId))
			.Returns(new Tgs.BrandCertificate.Clients.BrandInfo
			{
				Id = player.BrandId,
				Code = "Test",
				Currency = "CNY",
				SmsEnabled = false,
				SmsRegisterFlowEnabled = true
			});

		_ = moqValidateCodeGenerator
			.Setup(fake => fake.GenerateCode())
			.Returns(validateCode);
		_ = moqValidateCodeGenerator
			.Setup(fake => fake.GenerateSecurityKey())
			.Returns(expectedSecurityKey);

		_ = moqValidateCodeOperator
			.Setup(fake => fake.SaveValidateDataAsync(
				It.Is<string>(x => x == validateCode),
				It.Is<string>(x => x == expectedSecurityKey),
				It.IsAny<PhoneValidateData>()))
			.Returns(ValueTask.FromResult(true));

		var expectedShortMessage = $"Your ID verification code is : {validateCode}";

		var actualSecurityKey = await sut.GetAsync(command);

		moqSmsServiceClient.Verify(
			smsClient => smsClient.SendShortMessageAsync(
				It.Is<string>(x => x == $"{areaCode}{phone}"),
				It.Is<string>(x => x == expectedShortMessage),
				It.IsAny<Guid>(),
				It.IsAny<string>(),
				It.IsAny<string>(),
				It.IsAny<CancellationToken>()),
			Times.Exactly(0));

		ClassicAssert.AreEqual(expectedSecurityKey, actualSecurityKey);
	}
}