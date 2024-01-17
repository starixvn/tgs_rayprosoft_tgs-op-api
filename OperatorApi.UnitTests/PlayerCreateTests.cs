#nullable enable

using FluentValidation.TestHelper;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.BrandCertificate.Clients;
using Tgs.PlayerService.Client;
using Tgs.VipLevelService.Clients;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Apis.OperatorApi.Validators;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Exceptions;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Player;
using TGS.Services.Operator.PlayerRegisterFlow;
using BrandInfo = Tgs.BrandCertificate.Clients.BrandInfo;
using PlayerInfo = Tgs.PlayerService.Client.PlayerInfo;

namespace TGS.Tests.OperatorApi.UnitTests;

public class PlayerCreateTests
{
	[Test]
	public async Task Get_Player_Current_Register_Step()
	{
		var fakeBrandRegisterFlowDefinitionManager = Substitute.For<IBrandRegisterFlowDefinitionManager>();

		var sut = new GetCurrentPlayerRegisterFlowStepQueryService(
			fakeBrandRegisterFlowDefinitionManager);

		var query = new GetCurrentPlayerRegisterFlowStepQuery(Guid.NewGuid(), null);

		var flowDefinition = new RegisterFlowDefinition(NullLogger<RegisterFlow>.Instance);

		var fakeCreatePlayerStep = new StubRegisterFlowStep()
		{
			Type = "AAA"
		};

		fakeCreatePlayerStep.SetProcessInputDelegate((_, _, _) => Task.FromResult(
			new RegisterFlowProcessResult(true, null, null)));

		flowDefinition.AddStep(fakeCreatePlayerStep);

		_ = fakeBrandRegisterFlowDefinitionManager.GetRegisterFlowDefinitionAsync(Arg.Is(query.BrandId))
			.Returns(flowDefinition);

		var actual = await sut.GetAsync(query);

		Assert.AreEqual("AAA", actual);
	}

	[Test]
	public void Register_Player_With_Agent_InvitationCode_Not_Found_Should_Throw_Error()
	{
		var fakeAgentRepository = Substitute.For<IAgentRepository>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeVipLevelServiceClient = Substitute.For<IVipLevelServiceClient>();

		var sut = new CreateAgentInvitationPlayerCommandService(
			fakeAgentRepository,
			fakePlayerServiceClient,
			fakeBrandServiceClient,
			fakeVipLevelServiceClient);

		var brandId = Guid.NewGuid();
		var invitationCode = "6rd4mq";

		var query = new CreatePlayerWithAgentInvitationCodeCommand(
			brandId,
			"username",
			"password",
			"CNY",
			invitationCode,
			false);

		_ = fakeAgentRepository
			.GetAgentInfoByInvitationCodeAsync(
				Arg.Is(invitationCode))
			.Returns((AgentInfo?)null);

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			 .Returns(
				 new BrandInfo
				 {
					 Id = brandId,
					 Code = "code",
					 Currency = "CNY"
				 });

		_ = fakeBrandServiceClient.GetBrandBetLimitIdAsync(Arg.Is(brandId))
			.Returns(100);

		var vipLevelInfo = new Tgs.VipLevelService.Common.ViewModels.VipLevelInfoViewModel(
			Id: Guid.NewGuid(),
			Level: 1,
			BrandId: brandId,
			LevelUpAccumulateBetAmount: 0M,
			LevelUpAccumulateDeposit: 0M,
			LevelKeepAccumulateBetAmount: 0M,
			LevelKeepAccumulateDeposit: 0M,
			LevelKeepCondition: false,
			BirthBenefit: 0M,
			PromotionBenefit: 0M,
			QuarterlyBenefit: 0M,
			LiveDealerRebate: 0M,
			SlotRebate: 0M,
			LotteryRebate: 0M,
			CardGameRebate: 0M,
			SportsRebate: 0M,
			VirtualSportsRebate: 0M,
			WithdrawTransactionsPerDay: -1,
			WithdrawTotalAmountPerDay: -1m);
		_ = fakeVipLevelServiceClient.VipLevelGetAsync(
			brandId: Arg.Is(brandId),
			level: Arg.Is(1))
			.Returns(vipLevelInfo);

		// act
		_ = Assert.ThrowsAsync<InvalidInvitationCodeException>(
			async () => await sut.GetAsync(query));
	}

	[Test]
	public async Task Register_Player_With_Existent_InvitationCode()
	{
		// arrange
		var fakeAgentRepository = Substitute.For<IAgentRepository>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeVipLevelServiceClient = Substitute.For<IVipLevelServiceClient>();

		var sut = new CreateAgentInvitationPlayerCommandService(
			fakeAgentRepository,
			fakePlayerServiceClient,
			fakeBrandServiceClient,
			fakeVipLevelServiceClient);

		var brandId = Guid.NewGuid();
		var invitationCode = "6rd4mq";
		var agentId = Guid.NewGuid();

		var command = new CreatePlayerWithAgentInvitationCodeCommand(
			brandId,
			"username",
			"password",
			"CNY",
			invitationCode,
			false);

		_ = fakeAgentRepository
			.GetAgentInfoByInvitationCodeAsync(
				Arg.Is(invitationCode))
			.Returns(new AgentInfo(
				agentId,
				"AgentUserName",
				invitationCode,
				AgentStatus.Enabled));

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			.Returns(new BrandInfo
			{
				Id = brandId,
				Code = "code",
				Currency = "CNY"
			});

		_ = fakePlayerServiceClient.CreatePlayerAsync(Arg.Any<CreatePlayerData>())
			.Returns("TestPlayerCode");

		_ = fakeBrandServiceClient.GetBrandBetLimitIdAsync(Arg.Is(brandId))
			.Returns(100);

		var vipLevelInfo = new Tgs.VipLevelService.Common.ViewModels.VipLevelInfoViewModel(
			Id: Guid.NewGuid(),
			Level: 1,
			BrandId: brandId,
			LevelUpAccumulateBetAmount: 0M,
			LevelUpAccumulateDeposit: 0M,
			LevelKeepAccumulateBetAmount: 0M,
			LevelKeepAccumulateDeposit: 0M,
			LevelKeepCondition: false,
			BirthBenefit: 0M,
			PromotionBenefit: 0M,
			QuarterlyBenefit: 0M,
			LiveDealerRebate: 0M,
			SlotRebate: 0M,
			LotteryRebate: 0M,
			CardGameRebate: 0M,
			SportsRebate: 0M,
			VirtualSportsRebate: 0M,
			WithdrawTransactionsPerDay: -1,
			WithdrawTotalAmountPerDay: -1m);
		_ = fakeVipLevelServiceClient.VipLevelGetAsync(
			brandId: Arg.Is(brandId),
			level: Arg.Is(1))
			.Returns(vipLevelInfo);

		var resultPlayerCode = "TestPlayerCode";
		_ = fakePlayerServiceClient.CreatePlayerAsync(
			Arg.Is<CreatePlayerData>(data => data.BrandId == command.BrandId && data.Username == command.UserName && data.AgentId == agentId))
			.Returns(resultPlayerCode);

		// act
		var actual = await sut.GetAsync(command);

		// assert
		Assert.IsTrue(actual.IsNew);

		_ = fakeVipLevelServiceClient.Received(1)
			.PlayerVipLevelRegisterAsync(
			playerCode: Arg.Is(resultPlayerCode),
			vipLevel: Arg.Is(vipLevelInfo.Level),
			autoState: Arg.Is(Tgs.VipLevelService.Common.PlayerVipLevelAutoState.Enable),
			cancellationToken: Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task Register_Player_With_Existent_Upper_PlayerCode()
	{
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeVipLevelServiceClient = Substitute.For<IVipLevelServiceClient>();

		var sut = new CreatePlayerInvitationPlayerCommandService(
			fakePlayerServiceClient,
			fakeBrandServiceClient,
			fakeVipLevelServiceClient);

		var brandId = Guid.NewGuid();
		var upperPlayerCode = "upper";

		var command = new CreatePlayerWithPlayerInvitationCommand(
			brandId,
			"username",
			"password",
			"CNY",
			upperPlayerCode,
			false);

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			.Returns(ValueTask.FromResult<BrandInfo?>(
				new BrandInfo
				{
					Id = brandId,
					Code = "code",
					Currency = "CNY"
				}));

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(upperPlayerCode))
			.Returns(new PlayerInfo
			{
				PlayerCode = upperPlayerCode,
				BrandId = brandId
			});

		_ = fakeBrandServiceClient.GetBrandBetLimitIdAsync(Arg.Is(brandId))
			.Returns(100);

		var vipLevelInfo = new Tgs.VipLevelService.Common.ViewModels.VipLevelInfoViewModel(
			Id: Guid.NewGuid(),
			Level: 1,
			BrandId: brandId,
			LevelUpAccumulateBetAmount: 0M,
			LevelUpAccumulateDeposit: 0M,
			LevelKeepAccumulateBetAmount: 0M,
			LevelKeepAccumulateDeposit: 0M,
			LevelKeepCondition: false,
			BirthBenefit: 0M,
			PromotionBenefit: 0M,
			QuarterlyBenefit: 0M,
			LiveDealerRebate: 0M,
			SlotRebate: 0M,
			LotteryRebate: 0M,
			CardGameRebate: 0M,
			SportsRebate: 0M,
			VirtualSportsRebate: 0M,
			WithdrawTransactionsPerDay: -1,
			WithdrawTotalAmountPerDay: -1m);
		_ = fakeVipLevelServiceClient.VipLevelGetAsync(
			brandId: Arg.Is(brandId),
			level: Arg.Is(1))
			.Returns(vipLevelInfo);

		var resultPlayerCode = "TestPlayerCode";
		_ = fakePlayerServiceClient.CreatePlayerAsync(
			Arg.Is<CreatePlayerData>(data => data.BrandId == command.BrandId && data.Username == command.UserName))
			.Returns(resultPlayerCode);

		// act
		var actual = await sut.GetAsync(command);

		// assert
		Assert.IsTrue(actual.IsNew);

		_ = fakeVipLevelServiceClient.Received(1)
			.PlayerVipLevelRegisterAsync(
			playerCode: Arg.Is(resultPlayerCode),
			vipLevel: Arg.Is(vipLevelInfo.Level),
			autoState: Arg.Is(Tgs.VipLevelService.Common.PlayerVipLevelAutoState.Enable),
			cancellationToken: Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task Register_Player_With_No_InvitationCode()
	{
		// arrange
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeVipLevelServiceClient = Substitute.For<IVipLevelServiceClient>();

		var sut = new CreatePlayerCommandService(
			fakePlayerServiceClient,
			fakeBrandServiceClient,
			fakeVipLevelServiceClient);

		var brandId = Guid.NewGuid();
		var password = "password";

		var command = new CreatePlayerCommand(
			brandId,
			"username",
			password,
			"CNY",
			false);

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			.Returns(ValueTask.FromResult<BrandInfo?>(
				new BrandInfo
				{
					Id = brandId,
					Code = "code",
					Currency = "CNY"
				}));

		_ = fakeBrandServiceClient.GetBrandBetLimitIdAsync(Arg.Is(brandId))
			.Returns(100);

		var vipLevelInfo = new Tgs.VipLevelService.Common.ViewModels.VipLevelInfoViewModel(
			Id: Guid.NewGuid(),
			Level: 1,
			BrandId: brandId,
			LevelUpAccumulateBetAmount: 0M,
			LevelUpAccumulateDeposit: 0M,
			LevelKeepAccumulateBetAmount: 0M,
			LevelKeepAccumulateDeposit: 0M,
			LevelKeepCondition: false,
			BirthBenefit: 0M,
			PromotionBenefit: 0M,
			QuarterlyBenefit: 0M,
			LiveDealerRebate: 0M,
			SlotRebate: 0M,
			LotteryRebate: 0M,
			CardGameRebate: 0M,
			SportsRebate: 0M,
			VirtualSportsRebate: 0M,
			WithdrawTransactionsPerDay: -1,
			WithdrawTotalAmountPerDay: -1m);
		_ = fakeVipLevelServiceClient.VipLevelGetAsync(
			brandId: Arg.Is(brandId),
			level: Arg.Is(1))
			.Returns(vipLevelInfo);

		var resultPlayerCode = "TestPlayerCode";
		_ = fakePlayerServiceClient.CreatePlayerAsync(
			Arg.Is<CreatePlayerData>(data => data.BrandId == command.BrandId && data.Username == command.UserName))
			.Returns(resultPlayerCode);

		// act
		var actual = await sut.GetAsync(command);

		// assert
		Assert.IsTrue(actual.IsNew);

		_ = fakeVipLevelServiceClient.Received(1)
			.PlayerVipLevelRegisterAsync(
			playerCode: Arg.Is(resultPlayerCode),
			vipLevel: Arg.Is(vipLevelInfo.Level),
			autoState: Arg.Is(Tgs.VipLevelService.Common.PlayerVipLevelAutoState.Enable),
			cancellationToken: Arg.Any<CancellationToken>());
	}

	[Test]
	public void Register_Player_With_Non_Existent_InvitationCode()
	{
		// arrange
		var fakeAgentRepository = Substitute.For<IAgentRepository>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeVipLevelServiceClient = Substitute.For<IVipLevelServiceClient>();

		var sut = new CreateAgentInvitationPlayerCommandService(
			fakeAgentRepository,
			fakePlayerServiceClient,
			fakeBrandServiceClient,
			fakeVipLevelServiceClient);

		var brandId = Guid.NewGuid();
		var password = "password";
		var invitationCode = "NonExistentCode";

		var query = new CreatePlayerWithAgentInvitationCodeCommand(
			brandId,
			"username",
			password,
			"CNY",
			invitationCode,
			false);

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			.Returns(new BrandInfo
			{
				Id = brandId,
				Code = "code",
				Currency = "CNY"
			});

		_ = fakeBrandServiceClient.GetBrandBetLimitIdAsync(Arg.Is(brandId))
			.Returns(100);

		var vipLevelInfo = new Tgs.VipLevelService.Common.ViewModels.VipLevelInfoViewModel(
			Id: Guid.NewGuid(),
			Level: 1,
			BrandId: brandId,
			LevelUpAccumulateBetAmount: 0M,
			LevelUpAccumulateDeposit: 0M,
			LevelKeepAccumulateBetAmount: 0M,
			LevelKeepAccumulateDeposit: 0M,
			LevelKeepCondition: false,
			BirthBenefit: 0M,
			PromotionBenefit: 0M,
			QuarterlyBenefit: 0M,
			LiveDealerRebate: 0M,
			SlotRebate: 0M,
			LotteryRebate: 0M,
			CardGameRebate: 0M,
			SportsRebate: 0M,
			VirtualSportsRebate: 0M,
			WithdrawTransactionsPerDay: -1,
			WithdrawTotalAmountPerDay: -1m);
		_ = fakeVipLevelServiceClient.VipLevelGetAsync(
			brandId: Arg.Is(brandId),
			level: Arg.Is(1))
			.Returns(vipLevelInfo);

		_ = Assert.ThrowsAsync<InvalidInvitationCodeException>(async () => await sut.GetAsync(query));
	}

	[Test]
	public void Register_Player_With_Non_Existent_Upper_PlayerCode()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeVipLevelServiceClient = Substitute.For<IVipLevelServiceClient>();

		var sut = new CreatePlayerInvitationPlayerCommandService(
			fakePlayerServiceClient,
			fakeBrandServiceClient,
			fakeVipLevelServiceClient);

		var brandId = Guid.NewGuid();
		var upperPlayerCode = "upper";

		var command = new CreatePlayerWithPlayerInvitationCommand(
			brandId,
			"username",
			"password",
			"CNY",
			upperPlayerCode,
			false);

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			  .Returns(new BrandInfo
			  {
				  Id = brandId,
				  Code = "code",
				  Currency = "CNY"
			  });

		_ = fakeBrandServiceClient.GetBrandBetLimitIdAsync(Arg.Is(brandId))
			.Returns(100);

		var vipLevelInfo = new Tgs.VipLevelService.Common.ViewModels.VipLevelInfoViewModel(
			Id: Guid.NewGuid(),
			Level: 1,
			BrandId: brandId,
			LevelUpAccumulateBetAmount: 0M,
			LevelUpAccumulateDeposit: 0M,
			LevelKeepAccumulateBetAmount: 0M,
			LevelKeepAccumulateDeposit: 0M,
			LevelKeepCondition: false,
			BirthBenefit: 0M,
			PromotionBenefit: 0M,
			QuarterlyBenefit: 0M,
			LiveDealerRebate: 0M,
			SlotRebate: 0M,
			LotteryRebate: 0M,
			CardGameRebate: 0M,
			SportsRebate: 0M,
			VirtualSportsRebate: 0M,
			WithdrawTransactionsPerDay: -1,
			WithdrawTotalAmountPerDay: -1m);
		_ = fakeVipLevelServiceClient.VipLevelGetAsync(
			brandId: Arg.Is(brandId),
			level: Arg.Is(1))
			.Returns(vipLevelInfo);

		_ = Assert.ThrowsAsync<InvalidInvitationCodeException>(async () => await sut.GetAsync(command));
	}

	[Test]
	public void Register_Player_With_Other_Brand_Upper_PlayerCode_Should_Throw_Error()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeVipLevelServiceClient = Substitute.For<IVipLevelServiceClient>();

		var sut = new CreatePlayerInvitationPlayerCommandService(
			fakePlayerServiceClient,
			fakeBrandServiceClient,
			fakeVipLevelServiceClient);

		var brandId = Guid.NewGuid();
		var upperPlayerCode = "upper";

		var query = new CreatePlayerWithPlayerInvitationCommand(
			brandId,
			"username",
			"password",
			"CNY",
			upperPlayerCode,
			false);

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			.Returns(ValueTask.FromResult<BrandInfo?>(
				new BrandInfo
				{
					Id = brandId,
					Code = "code",
					Currency = "CNY"
				}));

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(upperPlayerCode))
			.Returns(new PlayerInfo
			{
				PlayerCode = upperPlayerCode,
				BrandId = Guid.NewGuid()
			});

		_ = fakeBrandServiceClient.GetBrandBetLimitIdAsync(Arg.Is(brandId))
			.Returns(100);

		var vipLevelInfo = new Tgs.VipLevelService.Common.ViewModels.VipLevelInfoViewModel(
			Id: Guid.NewGuid(),
			Level: 1,
			BrandId: brandId,
			LevelUpAccumulateBetAmount: 0M,
			LevelUpAccumulateDeposit: 0M,
			LevelKeepAccumulateBetAmount: 0M,
			LevelKeepAccumulateDeposit: 0M,
			LevelKeepCondition: false,
			BirthBenefit: 0M,
			PromotionBenefit: 0M,
			QuarterlyBenefit: 0M,
			LiveDealerRebate: 0M,
			SlotRebate: 0M,
			LotteryRebate: 0M,
			CardGameRebate: 0M,
			SportsRebate: 0M,
			VirtualSportsRebate: 0M,
			WithdrawTransactionsPerDay: -1,
			WithdrawTotalAmountPerDay: -1m);
		_ = fakeVipLevelServiceClient.VipLevelGetAsync(
			brandId: Arg.Is(brandId),
			level: Arg.Is(1))
			.Returns(vipLevelInfo);

		// act
		_ = Assert.ThrowsAsync<InvalidInvitationCodeException>(
			async () => await sut.GetAsync(query));
	}

	[Test]
	public void Verify_Player_Account_Input_Max_Length()
	{
		// arrange
		var sut = new CreatePlayerRequestValidator();

		var request = new CreatePlayerRequest
		{
			Password = "123456",
			UserName = "123456789012345678901234567890123",
			AgentInvitationCode = string.Empty,
			IsTestPlayer = false
		};

		// act
		var actual = sut.TestValidate(request);
		// assert
		_ = actual.ShouldHaveValidationErrorFor(request => request.UserName);
	}

	[Test]
	public void Verify_Player_Account_Input_Min_Length()
	{
		// arrange
		var sut = new CreatePlayerRequestValidator();

		var request = new CreatePlayerRequest
		{
			Password = "123456",
			UserName = "user",
			AgentInvitationCode = string.Empty,
			IsTestPlayer = false
		};

		// act
		var actual = sut.TestValidate(request);

		// assert
		_ = actual.ShouldHaveValidationErrorFor(request => request.UserName);
	}

	[Test]
	public void Verify_Player_Account_With_Special_Character()
	{
		// arrange
		var sut = new CreatePlayerRequestValidator();

		var request = new CreatePlayerRequest
		{
			Password = "123456",
			UserName = "abc123_",
			AgentInvitationCode = string.Empty,
			IsTestPlayer = false
		};

		// act
		var actual = sut.TestValidate(request);
		// assert
		_ = actual.ShouldHaveValidationErrorFor(request => request.UserName);
	}

	[Test]
	public void Verify_Player_Password_Input_Max_Length()
	{
		// arrange
		var sut = new CreatePlayerRequestValidator();

		var request = new CreatePlayerRequest
		{
			Password = "123456789012345678901",
			UserName = "username",
			AgentInvitationCode = string.Empty,
			IsTestPlayer = false
		};

		// act
		var actual = sut.TestValidate(request);
		// assert
		_ = actual.ShouldHaveValidationErrorFor(request => request.Password);
	}

	[Test]
	public void Verify_Player_Password_Input_Min_Length()
	{
		// arrange
		var sut = new CreatePlayerRequestValidator();

		var request = new CreatePlayerRequest
		{
			Password = "1234",
			UserName = "username",
			AgentInvitationCode = string.Empty,
			IsTestPlayer = false
		};

		// act
		var actual = sut.TestValidate(request);
		// assert
		_ = actual.ShouldHaveValidationErrorFor(request => request.Password);
	}
}
