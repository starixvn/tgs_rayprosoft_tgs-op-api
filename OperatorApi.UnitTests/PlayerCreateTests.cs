#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Apis.OperatorApi.Validators;
using Tgs.BrandCertificate.Clients;
using Tgs.Core.Infrastructures.Security;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Exceptions;
using Tgs.PlayerService.Client;
using TGS.Services.Operations;
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
		var brandId = Guid.NewGuid();
		var invitationCode = "6rd4mq";

		var fakeAgentRepository = Substitute.For<IAgentRepository>();
		var fakeBrandOperations = Substitute.For<IBrandOperations>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

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

		var vipLevelInfos = new[]
		{
			new VipLevelInfo(Guid.NewGuid(), 1)
		};

		_ = fakeBrandServiceClient.GetVipLevelInfosAsync(Arg.Is(brandId)).Returns(vipLevelInfos.ToAsyncEnumerable());

		var sut = new CreateAgentInvitationPlayerCommandService(
			fakeBrandOperations,
			fakeAgentRepository,
			fakePlayerServiceClient,
			fakeBrandServiceClient);

		var query = new CreatePlayerWithAgentInvitationCodeCommand(
			brandId,
			"username",
			"password",
			"CNY",
			invitationCode,
			false);

		// act
		_ = Assert.ThrowsAsync<InvalidInvitationCodeException>(
			async () => await sut.GetAsync(query));
	}

	[Test]
	public async Task Register_Player_With_Existent_InvitationCode()
	{
		// arrange
		var brandId = Guid.NewGuid();
		var invitationCode = "6rd4mq";
		var agentId = Guid.NewGuid();

		var fakeAgentRepository = Substitute.For<IAgentRepository>();
		var fakeBrandOperations = Substitute.For<IBrandOperations>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

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

		var vipLevelInfos = new[]
		{
			new VipLevelInfo(Guid.NewGuid(), 1)
		};

		_ = fakeBrandServiceClient.GetVipLevelInfosAsync(Arg.Is(brandId)).Returns(vipLevelInfos.ToAsyncEnumerable());

		var sut = new CreateAgentInvitationPlayerCommandService(
			fakeBrandOperations,
			fakeAgentRepository,
			fakePlayerServiceClient,
			fakeBrandServiceClient);

		var command = new CreatePlayerWithAgentInvitationCodeCommand(
			brandId,
			"username",
			"password",
			"CNY",
			invitationCode,
			false);

		// act
		var actual = await sut.GetAsync(command);

		// assert
		Assert.IsTrue(actual.IsNew);

		_ = fakePlayerServiceClient.Received(1)
			.CreatePlayerAsync(Arg.Is<CreatePlayerData>(data => data.BrandId == command.BrandId
				&& data.Username == command.UserName
				&& data.AgentId == agentId));
	}

	[Test]
	public async Task Register_Player_With_Existent_Upper_PlayerCode()
	{
		var brandId = Guid.NewGuid();
		var upperPlayerCode = "upper";

		var fakePlayerOperations = Substitute.For<IPlayerOperations>();
		var fakePasswordHandler = Substitute.For<IPasswordHandler>();
		var fakeBrandOperations = Substitute.For<IBrandOperations>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

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

		var vipLevelInfos = new[]
		{
			new VipLevelInfo(Guid.NewGuid(), 1)
		};

		_ = fakeBrandServiceClient.GetVipLevelInfosAsync(Arg.Is(brandId)).Returns(vipLevelInfos.ToAsyncEnumerable());

		var sut = new CreatePlayerInvitationPlayerCommandService(
			fakePlayerOperations,
			fakePasswordHandler,
			fakeBrandOperations,
			fakePlayerServiceClient,
			fakeBrandServiceClient);

		var command = new CreatePlayerWithPlayerInvitationCommand(
			brandId,
			"username",
			"password",
			"CNY",
			upperPlayerCode,
			false);

		var actual = await sut.GetAsync(command);

		// assert
		Assert.IsTrue(actual.IsNew);

		_ = fakePlayerServiceClient.Received(1)
			.CreatePlayerAsync(Arg.Is<CreatePlayerData>(data => data.BrandId == command.BrandId
				&& data.Username == command.UserName
				&& data.UpperPlayerCode == command.UpperPlayerCode));
	}

	[Test]
	public async Task Register_Player_With_No_InvitationCode()
	{
		// arrange
		var brandId = Guid.NewGuid();

		var fakeBrandOperations = Substitute.For<IBrandOperations>();

		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			.Returns(ValueTask.FromResult<BrandInfo?>(
				new BrandInfo
				{
					Id = brandId,
					Code = "code",
					Currency = "CNY"
				}));

		var vipLevelInfos = new[]
		{
			new VipLevelInfo(Guid.NewGuid(), 1)
		};

		_ = fakeBrandServiceClient.GetVipLevelInfosAsync(Arg.Is(brandId)).Returns(vipLevelInfos.ToAsyncEnumerable());

		var sut = new CreatePlayerCommandService(
			fakeBrandOperations,
			fakePlayerServiceClient,
			fakeBrandServiceClient);

		var password = "password";

		var command = new CreatePlayerCommand(
			brandId,
			"username",
			password,
			"CNY",
			false);

		// act
		var actual = await sut.GetAsync(command);

		// assert
		Assert.IsTrue(actual.IsNew);

		_ = fakePlayerServiceClient.Received(1)
			.CreatePlayerAsync(Arg.Is<CreatePlayerData>(data => data.BrandId == command.BrandId
				&& data.Username == command.UserName));
	}

	[Test]
	public void Register_Player_With_Non_Existent_InvitationCode()
	{
		// arrange
		var brandId = Guid.NewGuid();

		var fakeBrandOperations = Substitute.For<IBrandOperations>();
		var fakeAgentRepository = Substitute.For<IAgentRepository>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			.Returns(new BrandInfo
			{
				Id = brandId,
				Code = "code",
				Currency = "CNY"
			});

		var vipLevelInfos = new[]
		{
			new VipLevelInfo(Guid.NewGuid(), 1)
		};

		_ = fakeBrandServiceClient.GetVipLevelInfosAsync(Arg.Is(brandId)).Returns(vipLevelInfos.ToAsyncEnumerable());

		var sut = new CreateAgentInvitationPlayerCommandService(
			fakeBrandOperations,
			fakeAgentRepository,
			fakePlayerServiceClient,
			fakeBrandServiceClient);

		var password = "password";
		var invitationCode = "NonExistentCode";

		var query = new CreatePlayerWithAgentInvitationCodeCommand(
			brandId,
			"username",
			password,
			"CNY",
			invitationCode,
			false);

		_ = Assert.ThrowsAsync<InvalidInvitationCodeException>(async () => await sut.GetAsync(query));
	}

	[Test]
	public void Register_Player_With_Non_Existent_Upper_PlayerCode()
	{
		var brandId = Guid.NewGuid();
		var upperPlayerCode = "upper";

		var fakePlayerOperations = Substitute.For<IPlayerOperations>();
		var fakePasswordHandler = Substitute.For<IPasswordHandler>();
		var fakeBrandOperations = Substitute.For<IBrandOperations>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			  .Returns(new BrandInfo
			  {
				  Id = brandId,
				  Code = "code",
				  Currency = "CNY"
			  });

		var vipLevelInfos = new[]
		{
			new VipLevelInfo(Guid.NewGuid(), 1)
		};

		_ = fakeBrandServiceClient.GetVipLevelInfosAsync(Arg.Is(brandId)).Returns(vipLevelInfos.ToAsyncEnumerable());

		var sut = new CreatePlayerInvitationPlayerCommandService(
			fakePlayerOperations,
			fakePasswordHandler,
			fakeBrandOperations,
			fakePlayerServiceClient,
			fakeBrandServiceClient);

		var command = new CreatePlayerWithPlayerInvitationCommand(
			brandId,
			"username",
			"password",
			"CNY",
			upperPlayerCode,
			false);

		_ = Assert.ThrowsAsync<InvalidInvitationCodeException>(async () => await sut.GetAsync(command));
	}

	[Test]
	public void Register_Player_With_Other_Brand_Upper_PlayerCode_Should_Throw_Error()
	{
		var brandId = Guid.NewGuid();
		var upperPlayerCode = "upper";

		var fakePlayerOperations = Substitute.For<IPlayerOperations>();
		var fakePasswordHandler = Substitute.For<IPasswordHandler>();
		var fakeBrandOperations = Substitute.For<IBrandOperations>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

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

		var vipLevelInfos = new[]
		{
			new VipLevelInfo(Guid.NewGuid(), 1)
		};

		_ = fakeBrandServiceClient.GetVipLevelInfosAsync(Arg.Is(brandId)).Returns(vipLevelInfos.ToAsyncEnumerable());

		var sut = new CreatePlayerInvitationPlayerCommandService(
			fakePlayerOperations,
			fakePasswordHandler,
			fakeBrandOperations,
			fakePlayerServiceClient,
			fakeBrandServiceClient);

		var query = new CreatePlayerWithPlayerInvitationCommand(
			brandId,
			"username",
			"password",
			"CNY",
			upperPlayerCode,
			false);

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
