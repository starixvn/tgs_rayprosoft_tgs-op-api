using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using Tgs.BrandCertificate.Clients;
using Tgs.Payment.Clients;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;
using TGS.Core.Infrastructures.Redis;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Player;
using TGS.Services.Operator.ApplicationServices.Withdraw;
using TGS.Services.Operator.PlayerRegisterFlow;
using static TGS.Services.Operator.PlayerRegisterFlow.CreatePlayerRegisterFlowStep;
using static TGS.Services.Operator.PlayerRegisterFlow.SetEmailRegisterFlowStep;
using static TGS.Services.Operator.PlayerRegisterFlow.SetPhoneRegisterFlowStep;
using static TGS.Services.Operator.PlayerRegisterFlow.SetWithdrawBankInfoRegisterFlowStep;
using PhoneData = TGS.Services.Operator.PlayerRegisterFlow.SetPhoneDontCareDuplicateRegisterFlowStep.PhoneData;

namespace TGS.Tests.OperatorApi.UnitTests;

public class RegisterFlowTests
{
	[Test]
	public void Add_Step_To_RegisterFlowDefinition()
	{
		var sut = new RegisterFlowDefinition(NullLogger<RegisterFlow>.Instance);

		var privateList = typeof(RegisterFlowDefinition)
			.GetField("_stepDefinitions", BindingFlags.NonPublic | BindingFlags.Instance)
			.GetValue(sut) as List<IRegisterFlowStep>;

		Assert.AreEqual(0, privateList.Count);

		sut.AddStep(new StubRegisterFlowStep());

		Assert.AreEqual(1, privateList.Count);
	}

	[Test]
	public async Task After_Call_ConfirmCompleted_Will_Remain_Undone_Steps()
	{
		var playerCode = "player1";

		var step1 = new StubRegisterFlowStep();
		step1.SetConfirmCompletedDelegate(_ => true);
		var step2 = new StubRegisterFlowStep();
		var step3 = new StubRegisterFlowStep();

		var sut = new RegisterFlow(new[] {
			step1,
			step2,
			step3
		}, NullLogger<RegisterFlow>.Instance);

		var actual = await sut.ConfirmCompletedAsync(playerCode);

		var privateStack = typeof(RegisterFlow)
			.GetField("_flowSteps", BindingFlags.NonPublic | BindingFlags.Instance)
			.GetValue(sut) as Queue<IRegisterFlowStep>;

		Assert.IsFalse(actual);
		Assert.AreEqual(2, privateStack.Count);
	}

	[Test]
	public void Build_RegisterFlow()
	{
		var sut = new RegisterFlowDefinition(NullLogger<RegisterFlow>.Instance);

		sut.AddStep(new StubRegisterFlowStep());

		var actual = sut.BuildRegisterFlow();

		var privateStack = typeof(RegisterFlow)
			.GetField("_flowSteps", BindingFlags.NonPublic | BindingFlags.Instance)
			.GetValue(actual) as Queue<IRegisterFlowStep>;

		Assert.AreEqual(1, privateStack.Count);
	}

	[Test]
	public void Create_RegisterFlowDefinition_From_RegisterFlowBuilder()
	{
		var serviceProvider = new ServiceCollection()
			.AddSingleton(new CreatePlayerRegisterFlowStep(
				Substitute.For<IGetService<CreatePlayerCommand, CreatePlayerInfo>>(),
				Substitute.For<IGetService<CreatePlayerWithAgentInvitationCodeCommand, CreatePlayerInfo>>(),
				Substitute.For<IGetService<CreatePlayerWithPlayerInvitationCommand, CreatePlayerInfo>>(),
				Substitute.For<IPlayerServiceClient>(),
				Substitute.For<IBrandServiceClient>()))
			.AddSingleton<StubRegisterFlowStep>()
			.AddSingleton(new FinalRegisterFlowStep(
				Substitute.For<IPlayerServiceClient>()))
			.AddSingleton<ILogger<RegisterFlow>>(NullLogger<RegisterFlow>.Instance)
			.BuildServiceProvider();
		var sut = new RegisterFlowBuilder(
			serviceProvider,
			new Dictionary<string, Type>
			{
				["Test1"] = typeof(StubRegisterFlowStep)
			}.ToImmutableDictionary());

		var actual = sut.LoadSettings(JsonDocument.Parse(@"[
            {
                ""type"": ""Test1""
            }]"));

		var privateList = typeof(RegisterFlowDefinition)
			.GetField("_stepDefinitions", BindingFlags.NonPublic | BindingFlags.Instance)
			.GetValue(actual) as List<IRegisterFlowStep>;

		Assert.IsNotNull(privateList);
		Assert.AreEqual(3, privateList.Count);
	}

	[Test]
	public async Task CreatePlayerRegisterStep_If_Player_Exists_ConfirmCompleted_Should_Return_True()
	{
		var fakeCreatePlayerService = Substitute.For<IGetService<CreatePlayerCommand, CreatePlayerInfo>>();
		var fakeCreateAgentInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithAgentInvitationCodeCommand, CreatePlayerInfo>>();
		var fakeCreatePlayerInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithPlayerInvitationCommand, CreatePlayerInfo>>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var sut = new CreatePlayerRegisterFlowStep(
			fakeCreatePlayerService,
			fakeCreateAgentInvitationPlayerService,
			fakeCreatePlayerInvitationPlayerService,
			fakePlayerServiceClient,
			fakeBrandServiceClient);

		var playerCode = "player1";

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(playerCode))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo());

		var actual = await sut.ConfirmCompletedAsync(playerCode);

		Assert.IsTrue(actual);
	}

	[Test]
	public async Task CreatePlayerRegisterStep_ProcessInput_With_InvitationCode()
	{
		var fakeCreatePlayerService = Substitute.For<IGetService<CreatePlayerCommand, CreatePlayerInfo>>();
		var fakeCreateAgentInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithAgentInvitationCodeCommand, CreatePlayerInfo>>();
		var fakeCreatePlayerInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithPlayerInvitationCommand, CreatePlayerInfo>>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var sut = new CreatePlayerRegisterFlowStep(
			fakeCreatePlayerService,
			fakeCreateAgentInvitationPlayerService,
			fakeCreatePlayerInvitationPlayerService,
			fakePlayerServiceClient,
			fakeBrandServiceClient);

		var brandId = Guid.NewGuid();

		var input = new CreatePlayerInputData
		{
			IsTestPlayer = false,
			UserName = "abc",
			Password = "1234",
			AgentInvitationCode = "aabbcc"
		};

		var locationInfo = new PlayerLocationInfo(
			null,
			"127.0.0.1",
			null,
			null);

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			.Returns(new Tgs.BrandCertificate.Clients.BrandInfo
			{
				Id = brandId,
				Code = "AAA",
				Currency = "CNY"
			});

		_ = fakeCreateAgentInvitationPlayerService.GetAsync(Arg.Is<CreatePlayerWithAgentInvitationCodeCommand>(
			x => x.BrandId == brandId
				&& x.IsTestPlayer == input.IsTestPlayer
				&& x.UserName == input.UserName
				&& x.Password == input.Password
				&& x.AgentInvitationCode == input.AgentInvitationCode))
			.Returns(new CreatePlayerInfo("test", true));

		var actual = await sut.ProcessInputAsync(
			brandId,
			JsonSerializer.SerializeToDocument(input),
			locationInfo);

		Assert.IsTrue(actual.IsCompleted);

		_ = fakePlayerServiceClient.Received(1)
			.SetPlayerStatusAsync(Arg.Is("test"), Arg.Is(PlayerStatus.RegisterUndone));
	}

	[Test]
	public async Task CreatePlayerRegisterStep_ProcessInput_With_InvitationCode_Header()
	{
		var fakeCreatePlayerService = Substitute.For<IGetService<CreatePlayerCommand, CreatePlayerInfo>>();
		var fakeCreateAgentInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithAgentInvitationCodeCommand, CreatePlayerInfo>>();
		var fakeCreatePlayerInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithPlayerInvitationCommand, CreatePlayerInfo>>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var sut = new CreatePlayerRegisterFlowStep(
			fakeCreatePlayerService,
			fakeCreateAgentInvitationPlayerService,
			fakeCreatePlayerInvitationPlayerService,
			fakePlayerServiceClient,
			fakeBrandServiceClient);

		var brandId = Guid.NewGuid();

		var input = new CreatePlayerRegisterFlowStep.CreatePlayerInputData
		{
			IsTestPlayer = false,
			UserName = "abc",
			Password = "1234"
		};

		var locationInfo = new PlayerLocationInfo(
			null,
			"127.0.0.1",
			null,
			"aabbcc");

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			.Returns(new Tgs.BrandCertificate.Clients.BrandInfo
			{
				Id = brandId,
				Code = "AAA",
				Currency = "CNY"
			});

		_ = fakeCreateAgentInvitationPlayerService.GetAsync(Arg.Is<CreatePlayerWithAgentInvitationCodeCommand>(
			x => x.BrandId == brandId
				&& x.IsTestPlayer == input.IsTestPlayer
				&& x.UserName == input.UserName
				&& x.Password == input.Password
				&& x.AgentInvitationCode == locationInfo.AgentInvitationCode))
			.Returns(new CreatePlayerInfo("test", true));

		var actual = await sut.ProcessInputAsync(
			brandId,
			JsonSerializer.SerializeToDocument(input),
			locationInfo);

		Assert.IsTrue(actual.IsCompleted);

		_ = fakePlayerServiceClient.Received(1)
			.SetPlayerStatusAsync(Arg.Is("test"), Arg.Is(PlayerStatus.RegisterUndone));
	}

	[Test]
	public async Task CreatePlayerRegisterStep_ProcessInput_With_UpperPlayer()
	{
		var fakeCreatePlayerService = Substitute.For<IGetService<CreatePlayerCommand, CreatePlayerInfo>>();
		var fakeCreateAgentInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithAgentInvitationCodeCommand, CreatePlayerInfo>>();
		var fakeCreatePlayerInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithPlayerInvitationCommand, CreatePlayerInfo>>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var sut = new CreatePlayerRegisterFlowStep(
			fakeCreatePlayerService,
			fakeCreateAgentInvitationPlayerService,
			fakeCreatePlayerInvitationPlayerService,
			fakePlayerServiceClient,
			fakeBrandServiceClient);

		var brandId = Guid.NewGuid();

		var input = new CreatePlayerRegisterFlowStep.CreatePlayerInputData
		{
			IsTestPlayer = false,
			UserName = "abc",
			Password = "1234",
			UpperPlayerCode = "player2"
		};

		var locationInfo = new PlayerLocationInfo(
			null,
			"127.0.0.1",
			null,
			null);

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			.Returns(new Tgs.BrandCertificate.Clients.BrandInfo
			{
				Id = brandId,
				Code = "AAA",
				Currency = "CNY"
			});

		_ = fakeCreatePlayerInvitationPlayerService.GetAsync(Arg.Is<CreatePlayerWithPlayerInvitationCommand>(
			x => x.BrandId == brandId
				&& x.IsTestPlayer == input.IsTestPlayer
				&& x.UserName == input.UserName
				&& x.Password == input.Password
				&& x.UpperPlayerCode == input.UpperPlayerCode))
			.Returns(new CreatePlayerInfo("test", true));

		var actual = await sut.ProcessInputAsync(
			brandId,
			JsonSerializer.SerializeToDocument(input),
			locationInfo);

		Assert.IsTrue(actual.IsCompleted);

		_ = fakePlayerServiceClient.Received(1)
			.SetPlayerStatusAsync(Arg.Is("test"), Arg.Is(PlayerStatus.RegisterUndone));
	}

	[Test]
	public async Task CreatePlayerRegisterStep_ProcessInput_Without_InvitationCode()
	{
		var fakeCreatePlayerService = Substitute.For<IGetService<CreatePlayerCommand, CreatePlayerInfo>>();
		var fakeCreateAgentInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithAgentInvitationCodeCommand, CreatePlayerInfo>>();
		var fakeCreatePlayerInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithPlayerInvitationCommand, CreatePlayerInfo>>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var sut = new CreatePlayerRegisterFlowStep(
			fakeCreatePlayerService,
			fakeCreateAgentInvitationPlayerService,
			fakeCreatePlayerInvitationPlayerService,
			fakePlayerServiceClient,
			fakeBrandServiceClient);

		var brandId = Guid.NewGuid();

		var input = new CreatePlayerRegisterFlowStep.CreatePlayerInputData
		{
			IsTestPlayer = false,
			UserName = "abc",
			Password = "1234"
		};

		var locationInfo = new PlayerLocationInfo(
			null,
			"127.0.0.1",
			null,
			null);

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			.Returns(new Tgs.BrandCertificate.Clients.BrandInfo
			{
				Id = brandId,
				Code = "AAA",
				Currency = "CNY"
			});

		_ = fakeCreatePlayerService.GetAsync(Arg.Is<CreatePlayerCommand>(
			x => x.BrandId == brandId
				&& x.IsTestPlayer == input.IsTestPlayer
				&& x.UserName == input.UserName
				&& x.Password == input.Password))
			.Returns(new CreatePlayerInfo("test", true));

		var actual = await sut.ProcessInputAsync(
			brandId,
			JsonSerializer.SerializeToDocument(input),
			locationInfo);

		Assert.IsTrue(actual.IsCompleted);

		_ = fakePlayerServiceClient.Received(1)
			.SetPlayerStatusAsync(Arg.Is("test"), Arg.Is(PlayerStatus.RegisterUndone));
	}

	[Test]
	public async Task Empty_Flow_Create_Player()
	{
		var flowTypes = new Dictionary<string, Type>
		{
		}.ToImmutableDictionary();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();

		var sut = new RegisterFlowBuilder(fakeServiceProvider, flowTypes);

		var flowSettings = JsonDocument.Parse("[]");

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ILogger<RegisterFlow>)))
			.Returns(NullLogger<RegisterFlow>.Instance);

		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var fakeCreatePlayerService = Substitute.For<IGetService<CreatePlayerCommand, CreatePlayerInfo>>();
		var fakeCreateAgentInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithAgentInvitationCodeCommand, CreatePlayerInfo>>();
		var fakeCreatePlayerInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithPlayerInvitationCommand, CreatePlayerInfo>>();

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(CreatePlayerRegisterFlowStep)))
			.Returns(new CreatePlayerRegisterFlowStep(
				fakeCreatePlayerService,
				fakeCreateAgentInvitationPlayerService,
				fakeCreatePlayerInvitationPlayerService,
				fakePlayerServiceClient,
				fakeBrandServiceClient));

		var fakePlayerServiceClient2 = Substitute.For<IPlayerServiceClient>();

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(FinalRegisterFlowStep)))
			.Returns(new FinalRegisterFlowStep(fakePlayerServiceClient2));

		var flowDefinition = sut.LoadSettings(flowSettings);

		var flow = flowDefinition.BuildRegisterFlow();

		var brandId = Guid.NewGuid();
		var inputData = new CreatePlayerInputData
		{
			UserName = "tester",
			Password = "123456"
		};
		var locationInfo = new PlayerLocationInfo(null, "127.0.0.1", null, null);

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			.Returns(new Tgs.BrandCertificate.Clients.BrandInfo
			{
				Id = brandId,
				Currency = "CNY"
			});

		_ = fakeCreatePlayerService.GetAsync(Arg.Is<CreatePlayerCommand>(
			cmd => cmd.UserName == inputData.UserName
				&& cmd.BrandId == brandId))
			.Returns(new CreatePlayerInfo("aabbcc", true));

		var expectedResponse = new CreatePlayerResponse
		{
			IsNew = true,
			PlayerCode = "aabbcc"
		};

		_ = fakePlayerServiceClient2.GetPlayerInfoAsync(Arg.Is(expectedResponse.PlayerCode))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				PlayerCode = expectedResponse.PlayerCode
			});

		_ = await flow.ConfirmCompletedAsync(null);
		var actual = await flow.ProcessInputAsync(
			brandId,
			JsonSerializer.SerializeToDocument(inputData),
			locationInfo);

		Assert.IsTrue(actual.IsCompleted);
		Assert.IsNull(actual.NextStepType);

		_ = fakePlayerServiceClient2.Received(1)
			.SetPlayerStatusAsync(Arg.Is(expectedResponse.PlayerCode), Arg.Is(PlayerStatus.Enabled));

		_ = fakePlayerServiceClient2.Received(1)
			.RecordRegisterIpAsync(
				Arg.Is(expectedResponse.PlayerCode),
				Arg.Any<string>(),
				Arg.Any<string>());
	}

	[Test]
	public async Task ExecutePlayerRegisterFlow()
	{
		var fakeBrandRegisterFlowDefinitionManager = Substitute.For<IBrandRegisterFlowDefinitionManager>();

		var sut = new ExecutePlayerRegisterFlowCommandService(fakeBrandRegisterFlowDefinitionManager);

		var command = new ExecutePlayerRegisterFlowCommand(
			Guid.NewGuid(),
			JsonDocument.Parse("{}"),
			new PlayerLocationInfo(null, "127.0.0.1", null, null));

		var flowDefinition = new RegisterFlowDefinition(NullLogger<RegisterFlow>.Instance);

		var fakeCreatePlayerStep = new StubRegisterFlowStep()
		{
			Type = "CreatePlayer"
		};

		fakeCreatePlayerStep.SetProcessInputDelegate((_, _, _) => Task.FromResult(
			new RegisterFlowProcessResult(true, null, null)));

		flowDefinition.AddStep(fakeCreatePlayerStep);

		_ = fakeBrandRegisterFlowDefinitionManager.GetRegisterFlowDefinitionAsync(Arg.Is(command.BrandId))
			.Returns(flowDefinition);

		var actual = await sut.GetAsync(command);

		Assert.IsTrue(actual.IsCompleted);
	}

	[Test]
	public async Task FinalRegisterFlowStep_ConfirmCompleted_PlayerStatus_Is_RegisterUndone_Should_Return_False()
	{
		var fakePlayerSericeClient = Substitute.For<IPlayerServiceClient>();

		var sut = new FinalRegisterFlowStep(fakePlayerSericeClient);

		var playerCode = "player1";

		_ = fakePlayerSericeClient.GetPlayerStatusAsync(Arg.Is(playerCode))
			.Returns(PlayerStatus.RegisterUndone);

		var actual = await sut.ConfirmCompletedAsync(playerCode);

		Assert.IsFalse(actual);
	}

	[Test]
	public async Task FinalRegisterFlowStep_ProcessInput()
	{
		var fakePlayerSericeClient = Substitute.For<IPlayerServiceClient>();

		var sut = new FinalRegisterFlowStep(fakePlayerSericeClient);

		var brandId = Guid.NewGuid();
		var input = JsonDocument.Parse("{}");
		var locationInfo = new PlayerLocationInfo("player1", "127.0.0.1", "localhost", null);

		_ = fakePlayerSericeClient.GetPlayerInfoAsync(Arg.Is(locationInfo.PlayerCode))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				PlayerCode = locationInfo.PlayerCode
			});

		var actual = await sut.ProcessInputAsync(brandId, input, locationInfo);

		Assert.IsTrue(actual.IsCompleted);

		_ = fakePlayerSericeClient.Received(1)
			.SetPlayerStatusAsync(
				Arg.Is(locationInfo.PlayerCode),
				Arg.Is(PlayerStatus.Enabled));

		_ = fakePlayerSericeClient.Received(1)
			.RecordRegisterIpAsync(
				Arg.Is(locationInfo.PlayerCode),
				Arg.Is(locationInfo.Ip),
				Arg.Is(locationInfo.HostName));
	}

	[Test]
	public async Task If_Has_Any_Undone_Step_ConfirmCompleted_Should_Return_True()
	{
		var sut = new RegisterFlow(Array.Empty<IRegisterFlowStep>(), NullLogger<RegisterFlow>.Instance);

		var actual = await sut.ConfirmCompletedAsync(null);

		Assert.IsTrue(actual);
	}

	[Test]
	public async Task PB_Flow_Input_Phone_Step()
	{
		var flowTypes = new Dictionary<string, Type>
		{
			["SetPhoneDontCareDuplicate"] = typeof(SetPhoneDontCareDuplicateRegisterFlowStep)
		}.ToImmutableDictionary();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();

		var sut = new RegisterFlowBuilder(fakeServiceProvider, flowTypes);

		var flowSettings = JsonDocument.Parse("""
            [
                { "type": "SetPhoneDontCareDuplicate" }
            ]
        """);

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ILogger<RegisterFlow>)))
			.Returns(NullLogger<RegisterFlow>.Instance);

		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		_ = fakeServiceProvider.GetService(Arg.Is(flowTypes["SetPhoneDontCareDuplicate"]))
			.Returns(new SetPhoneDontCareDuplicateRegisterFlowStep(
				fakePlayerServiceClient));

		var fakeCreatePlayerService = Substitute.For<IGetService<CreatePlayerCommand, CreatePlayerInfo>>();
		var fakeCreateAgentInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithAgentInvitationCodeCommand, CreatePlayerInfo>>();
		var fakeCreatePlayerInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithPlayerInvitationCommand, CreatePlayerInfo>>();

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(CreatePlayerRegisterFlowStep)))
			.Returns(new CreatePlayerRegisterFlowStep(
				fakeCreatePlayerService,
				fakeCreateAgentInvitationPlayerService,
				fakeCreatePlayerInvitationPlayerService,
				fakePlayerServiceClient,
				fakeBrandServiceClient));

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(FinalRegisterFlowStep)))
			.Returns(new FinalRegisterFlowStep(fakePlayerServiceClient));

		var flowDefinition = sut.LoadSettings(flowSettings);

		var flow = flowDefinition.BuildRegisterFlow();

		var brandId = Guid.NewGuid();
		var inputData = new PhoneData
		{
			AreaCode = "123",
			Phone = "12345678"
		};
		var locationInfo = new PlayerLocationInfo("player1", "127.0.0.1", null, null);

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(locationInfo.PlayerCode))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				PlayerCode = locationInfo.PlayerCode
			});

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			.Returns(new Tgs.BrandCertificate.Clients.BrandInfo
			{
				Id = brandId,
				Currency = "CNY"
			});

		fakePlayerServiceClient
			.When(fake => fake.SetPlayerPropertiesAsync(
				Arg.Is(locationInfo.PlayerCode),
				Arg.Is<IImmutableDictionary<PlayerPropertyName, string>>(dict => dict[PlayerPropertyName.AreaCode] == inputData.AreaCode
					&& dict[PlayerPropertyName.Phone] == inputData.Phone
					&& dict[PlayerPropertyName.PhoneAuthentication] == true.ToString())))
			.Do(_ =>
			{
				_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(locationInfo.PlayerCode))
					.Returns(new Tgs.PlayerService.Client.PlayerInfo
					{
						PlayerCode = locationInfo.PlayerCode,
						PhoneAuthentication = true
					});
			});

		_ = await flow.ConfirmCompletedAsync("player1");
		var actual = await flow.ProcessInputAsync(
			brandId,
			JsonSerializer.SerializeToDocument(inputData),
			locationInfo);

		Assert.IsTrue(actual.IsCompleted);
		Assert.IsNull(actual.NextStepType);
	}

	[Test]
	public async Task PB_Flow_Start_Step()
	{
		var flowTypes = new Dictionary<string, Type>
		{
			["SetPhoneDontCareDuplicate"] = typeof(SetPhoneDontCareDuplicateRegisterFlowStep)
		}.ToImmutableDictionary();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();

		var sut = new RegisterFlowBuilder(fakeServiceProvider, flowTypes);

		var flowSettings = JsonDocument.Parse("""
            [
                { "type": "SetPhoneDontCareDuplicate" }
            ]
        """);

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ILogger<RegisterFlow>)))
			.Returns(NullLogger<RegisterFlow>.Instance);

		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		_ = fakeServiceProvider.GetService(Arg.Is(flowTypes["SetPhoneDontCareDuplicate"]))
			.Returns(new SetPhoneDontCareDuplicateRegisterFlowStep(
				fakePlayerServiceClient));

		var fakeCreatePlayerService = Substitute.For<IGetService<CreatePlayerCommand, CreatePlayerInfo>>();
		var fakeCreateAgentInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithAgentInvitationCodeCommand, CreatePlayerInfo>>();
		var fakeCreatePlayerInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithPlayerInvitationCommand, CreatePlayerInfo>>();

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(CreatePlayerRegisterFlowStep)))
			.Returns(new CreatePlayerRegisterFlowStep(
				fakeCreatePlayerService,
				fakeCreateAgentInvitationPlayerService,
				fakeCreatePlayerInvitationPlayerService,
				fakePlayerServiceClient,
				fakeBrandServiceClient));

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(FinalRegisterFlowStep)))
			.Returns(new FinalRegisterFlowStep(fakePlayerServiceClient));

		var flowDefinition = sut.LoadSettings(flowSettings);

		var flow = flowDefinition.BuildRegisterFlow();

		var brandId = Guid.NewGuid();
		var inputData = new CreatePlayerInputData
		{
			UserName = "tester",
			Password = "123456"
		};
		var locationInfo = new PlayerLocationInfo(null, "127.0.0.1", null, null);

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			.Returns(new Tgs.BrandCertificate.Clients.BrandInfo
			{
				Id = brandId,
				Currency = "CNY"
			});

		_ = fakeCreatePlayerService.GetAsync(Arg.Is<CreatePlayerCommand>(
			cmd => cmd.UserName == inputData.UserName
				&& cmd.BrandId == brandId))
			.Returns(new CreatePlayerInfo("aabbcc", true));

		var expectedResponse = new CreatePlayerResponse
		{
			IsNew = true,
			PlayerCode = "aabbcc"
		};

		_ = await flow.ConfirmCompletedAsync(null);
		var actual = await flow.ProcessInputAsync(
			brandId,
			JsonSerializer.SerializeToDocument(inputData),
			locationInfo);

		Assert.IsFalse(actual.IsCompleted);
		Assert.AreEqual("SetPhoneDontCareDuplicate", actual.NextStepType);
	}

	[Test]
	public async Task Phone_Mail_BankInfo_Flow_InitMail_Step()
	{
		var flowTypes = new Dictionary<string, Type>
		{
			["InitPhone"] = typeof(SetPhoneRegisterFlowStep),
			["InitMail"] = typeof(SetEmailRegisterFlowStep),
			["SetWithdrawBankInfo"] = typeof(SetWithdrawBankInfoRegisterFlowStep)
		}.ToImmutableDictionary();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();

		var sut = new RegisterFlowBuilder(fakeServiceProvider, flowTypes);

		var flowSettings = JsonDocument.Parse("""
            [
                { "type": "InitPhone" },
                { "type": "InitMail" },
                { "type": "SetWithdrawBankInfo" }
            ]
        """);

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ILogger<RegisterFlow>)))
			.Returns(NullLogger<RegisterFlow>.Instance);

		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var fakeStartSetPhoneService = Substitute.For<IGetService<StartSetPlayerPhoneCommand, string>>();
		var fakeConfirmSetPhoneService = Substitute.For<ICommandService<CommitInitPlayerPhoneCommand>>();

		_ = fakeServiceProvider.GetService(Arg.Is(flowTypes["InitPhone"]))
			.Returns(new SetPhoneRegisterFlowStep(
				fakeBrandServiceClient,
				fakeStartSetPhoneService,
				fakeConfirmSetPhoneService,
				fakePlayerServiceClient));

		var fakeStartSetMailService = Substitute.For<IGetService<StartSetPlayerEmailCommand, string>>();
		var fakeCommitMailService = Substitute.For<ICommandService<CommitInitPlayerEmailCommand>>();

		_ = fakeServiceProvider.GetService(Arg.Is(flowTypes["InitMail"]))
			.Returns(new SetEmailRegisterFlowStep(
				fakeBrandServiceClient,
				fakeStartSetMailService,
				fakeCommitMailService,
				fakePlayerServiceClient));

		var fakeQueryPlayerBankInfoService = Substitute.For<IQueryService<GetPlayerBankInfosQuery, PlayerBankInfo>>();
		var fakeCreateBankInfoService = Substitute.For<ICommandService<CreateInitBankInfoCommand>>();

		_ = fakeServiceProvider.GetService(Arg.Is(flowTypes["SetWithdrawBankInfo"]))
			.Returns(new SetWithdrawBankInfoRegisterFlowStep(
				fakeQueryPlayerBankInfoService,
				fakeCreateBankInfoService));

		var fakeCreatePlayerService = Substitute.For<IGetService<CreatePlayerCommand, CreatePlayerInfo>>();
		var fakeCreateAgentInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithAgentInvitationCodeCommand, CreatePlayerInfo>>();
		var fakeCreatePlayerInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithPlayerInvitationCommand, CreatePlayerInfo>>();

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(CreatePlayerRegisterFlowStep)))
			.Returns(new CreatePlayerRegisterFlowStep(
				fakeCreatePlayerService,
				fakeCreateAgentInvitationPlayerService,
				fakeCreatePlayerInvitationPlayerService,
				fakePlayerServiceClient,
				fakeBrandServiceClient));

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(FinalRegisterFlowStep)))
			.Returns(new FinalRegisterFlowStep(fakePlayerServiceClient));

		var playerCode = "player1";
		var brandId = Guid.NewGuid();

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(playerCode))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				PlayerCode = playerCode,
				BrandId = brandId,
				PhoneAuthentication = true
			});

		var flowDefinition = sut.LoadSettings(flowSettings);

		var flow = flowDefinition.BuildRegisterFlow();

		var inputData = new InitEmailStepInputData
		{
			Data = JsonSerializer.SerializeToDocument(new EmailData
			{
				Email = "tester@test.com"
			})
		};
		var locationInfo = new PlayerLocationInfo(playerCode, "127.0.0.1", null, null);

		var expected = Guid.NewGuid().ToString();

		_ = fakeStartSetMailService.GetAsync(
			Arg.Is<StartSetPlayerEmailCommand>(cmd => cmd.Email == "tester@test.com"))
			.Returns(expected);

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			.Returns(new Tgs.BrandCertificate.Clients.BrandInfo
			{
				Id = brandId
			});

		_ = await flow.ConfirmCompletedAsync(playerCode);
		var actual = await flow.ProcessInputAsync(
			brandId,
			JsonSerializer.SerializeToDocument(inputData),
			locationInfo);

		Assert.IsFalse(actual.IsCompleted);
		Assert.AreEqual("InitMail", actual.NextStepType);
		Assert.AreEqual(expected, actual.StepResponse.Deserialize<string>());
	}

	[Test]
	public async Task Phone_Mail_BankInfo_Flow_InitPhone_Step()
	{
		var flowTypes = new Dictionary<string, Type>
		{
			["InitPhone"] = typeof(SetPhoneRegisterFlowStep),
			["InitMail"] = typeof(SetEmailRegisterFlowStep),
			["SetWithdrawBankInfo"] = typeof(SetWithdrawBankInfoRegisterFlowStep)
		}.ToImmutableDictionary();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();

		var sut = new RegisterFlowBuilder(fakeServiceProvider, flowTypes);

		var flowSettings = JsonDocument.Parse("""
            [
                { "type": "InitPhone" },
                { "type": "InitMail" },
                { "type": "SetWithdrawBankInfo" }
            ]
        """);

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ILogger<RegisterFlow>)))
			.Returns(NullLogger<RegisterFlow>.Instance);

		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var fakeStartSetPhoneService = Substitute.For<IGetService<StartSetPlayerPhoneCommand, string>>();
		var fakeConfirmSetPhoneService = Substitute.For<ICommandService<CommitInitPlayerPhoneCommand>>();

		_ = fakeServiceProvider.GetService(Arg.Is(flowTypes["InitPhone"]))
			.Returns(new SetPhoneRegisterFlowStep(
				fakeBrandServiceClient,
				fakeStartSetPhoneService,
				fakeConfirmSetPhoneService,
				fakePlayerServiceClient));

		var fakeStartSetMailService = Substitute.For<IGetService<StartSetPlayerEmailCommand, string>>();
		var fakeCommitMailService = Substitute.For<ICommandService<CommitInitPlayerEmailCommand>>();

		_ = fakeServiceProvider.GetService(Arg.Is(flowTypes["InitMail"]))
			.Returns(new SetEmailRegisterFlowStep(
				fakeBrandServiceClient,
				fakeStartSetMailService,
				fakeCommitMailService,
				fakePlayerServiceClient));

		var fakeQueryPlayerBankInfoService = Substitute.For<IQueryService<GetPlayerBankInfosQuery, PlayerBankInfo>>();
		var fakeCreateBankInfoService = Substitute.For<ICommandService<CreateInitBankInfoCommand>>();

		_ = fakeServiceProvider.GetService(Arg.Is(flowTypes["SetWithdrawBankInfo"]))
			.Returns(new SetWithdrawBankInfoRegisterFlowStep(
				fakeQueryPlayerBankInfoService,
				fakeCreateBankInfoService));

		var fakeCreatePlayerService = Substitute.For<IGetService<CreatePlayerCommand, CreatePlayerInfo>>();
		var fakeCreateAgentInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithAgentInvitationCodeCommand, CreatePlayerInfo>>();
		var fakeCreatePlayerInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithPlayerInvitationCommand, CreatePlayerInfo>>();

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(CreatePlayerRegisterFlowStep)))
			.Returns(new CreatePlayerRegisterFlowStep(
				fakeCreatePlayerService,
				fakeCreateAgentInvitationPlayerService,
				fakeCreatePlayerInvitationPlayerService,
				fakePlayerServiceClient,
				fakeBrandServiceClient));

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(FinalRegisterFlowStep)))
			.Returns(new FinalRegisterFlowStep(fakePlayerServiceClient));

		var playerCode = "player1";
		var brandId = Guid.NewGuid();

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(playerCode))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				PlayerCode = playerCode,
				BrandId = brandId
			});

		var flowDefinition = sut.LoadSettings(flowSettings);

		var flow = flowDefinition.BuildRegisterFlow();

		var inputData = new InitPhoneStepInputData
		{
			Data = JsonSerializer.SerializeToDocument(new PhoneData
			{
				AreaCode = "123",
				Phone = "12345678"
			})
		};
		var locationInfo = new PlayerLocationInfo(playerCode, "127.0.0.1", null, null);

		var expected = Guid.NewGuid().ToString();

		_ = fakeStartSetPhoneService
			.GetAsync(Arg.Is<StartSetPlayerPhoneCommand>(cmd => cmd.Phone == "12345678"
				&& cmd.AreaCode == "123"))
			.Returns(expected);

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			.Returns(new Tgs.BrandCertificate.Clients.BrandInfo
			{
				Id = brandId
			});

		_ = await flow.ConfirmCompletedAsync(playerCode);
		var actual = await flow.ProcessInputAsync(
			brandId,
			JsonSerializer.SerializeToDocument(inputData),
			locationInfo);

		Assert.IsFalse(actual.IsCompleted);
		Assert.AreEqual("InitPhone", actual.NextStepType);
		Assert.AreEqual(expected, actual.StepResponse.Deserialize<string>());
	}

	[Test]
	public async Task Phone_Mail_BankInfo_Flow_SetWithdrawBankInfo_Step()
	{
		var flowTypes = new Dictionary<string, Type>
		{
			["InitPhone"] = typeof(SetPhoneRegisterFlowStep),
			["InitMail"] = typeof(SetEmailRegisterFlowStep),
			["SetWithdrawBankInfo"] = typeof(SetWithdrawBankInfoRegisterFlowStep)
		}.ToImmutableDictionary();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();

		var sut = new RegisterFlowBuilder(fakeServiceProvider, flowTypes);

		var flowSettings = JsonDocument.Parse("""
            [
                { "type": "InitPhone" },
                { "type": "InitMail" },
                { "type": "SetWithdrawBankInfo" }
            ]
        """);

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ILogger<RegisterFlow>)))
			.Returns(NullLogger<RegisterFlow>.Instance);

		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var fakeStartSetPhoneService = Substitute.For<IGetService<StartSetPlayerPhoneCommand, string>>();
		var fakeConfirmSetPhoneService = Substitute.For<ICommandService<CommitInitPlayerPhoneCommand>>();

		_ = fakeServiceProvider.GetService(Arg.Is(flowTypes["InitPhone"]))
			.Returns(new SetPhoneRegisterFlowStep(
				fakeBrandServiceClient,
				fakeStartSetPhoneService,
				fakeConfirmSetPhoneService,
				fakePlayerServiceClient));

		var fakeStartSetMailService = Substitute.For<IGetService<StartSetPlayerEmailCommand, string>>();
		var fakeCommitMailService = Substitute.For<ICommandService<CommitInitPlayerEmailCommand>>();

		_ = fakeServiceProvider.GetService(Arg.Is(flowTypes["InitMail"]))
			.Returns(new SetEmailRegisterFlowStep(
				fakeBrandServiceClient,
				fakeStartSetMailService,
				fakeCommitMailService,
				fakePlayerServiceClient));

		var fakeQueryPlayerBankInfoService = Substitute.For<IQueryService<GetPlayerBankInfosQuery, PlayerBankInfo>>();
		var fakeCreateBankInfoService = Substitute.For<ICommandService<CreateInitBankInfoCommand>>();

		_ = fakeServiceProvider.GetService(Arg.Is(flowTypes["SetWithdrawBankInfo"]))
			.Returns(new SetWithdrawBankInfoRegisterFlowStep(
				fakeQueryPlayerBankInfoService,
				fakeCreateBankInfoService));

		var fakeCreatePlayerService = Substitute.For<IGetService<CreatePlayerCommand, CreatePlayerInfo>>();
		var fakeCreateAgentInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithAgentInvitationCodeCommand, CreatePlayerInfo>>();
		var fakeCreatePlayerInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithPlayerInvitationCommand, CreatePlayerInfo>>();

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(CreatePlayerRegisterFlowStep)))
			.Returns(new CreatePlayerRegisterFlowStep(
				fakeCreatePlayerService,
				fakeCreateAgentInvitationPlayerService,
				fakeCreatePlayerInvitationPlayerService,
				fakePlayerServiceClient,
				fakeBrandServiceClient));

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(FinalRegisterFlowStep)))
			.Returns(new FinalRegisterFlowStep(fakePlayerServiceClient));

		var playerCode = "player1";
		var brandId = Guid.NewGuid();

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(playerCode))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				PlayerCode = playerCode,
				BrandId = brandId,
				PhoneAuthentication = true,
				EmailAuthentication = true,
			});

		_ = fakePlayerServiceClient.GetPlayerStatusAsync(Arg.Is(playerCode))
			.Returns(PlayerStatus.RegisterUndone);

		var flowDefinition = sut.LoadSettings(flowSettings);

		var flow = flowDefinition.BuildRegisterFlow();

		var securityKey = Guid.NewGuid().ToString();

		var inputData = new InitEmailStepInputData
		{
			InputType = "Valid",
			Data = JsonSerializer.SerializeToDocument(new ConfirmEmailData
			{
				SecurityKey = securityKey,
				ValidateCode = "123456"
			})
		};
		var locationInfo = new PlayerLocationInfo(playerCode, "127.0.0.1", null, null);

		var expected = Guid.NewGuid().ToString();

		_ = fakeStartSetPhoneService
			.GetAsync(Arg.Is<StartSetPlayerPhoneCommand>(cmd => cmd.Phone == "12345678"
				&& cmd.AreaCode == "123"))
			.Returns(expected);

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			.Returns(new Tgs.BrandCertificate.Clients.BrandInfo
			{
				Id = brandId
			});

		_ = await flow.ConfirmCompletedAsync(playerCode);
		var actual = await flow.ProcessInputAsync(
			brandId,
			JsonSerializer.SerializeToDocument(inputData),
			locationInfo);

		Assert.IsTrue(actual.IsCompleted);
		Assert.IsNull(actual.NextStepType);

		_ = fakeCreateBankInfoService.Received(1)
			.ExecuteAsync(Arg.Is<CreateInitBankInfoCommand>(
				cmd => cmd.PlayerCode == playerCode
					&& cmd.BrandId == brandId));
	}

	[Test]
	public async Task Phone_Mail_BankInfo_Flow_Start_Step()
	{
		var flowTypes = new Dictionary<string, Type>
		{
			["InitPhone"] = typeof(SetPhoneRegisterFlowStep),
			["InitMail"] = typeof(SetEmailRegisterFlowStep),
			["SetWithdrawBankInfo"] = typeof(SetWithdrawBankInfoRegisterFlowStep)
		}.ToImmutableDictionary();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();

		var sut = new RegisterFlowBuilder(fakeServiceProvider, flowTypes);

		var flowSettings = JsonDocument.Parse("""
            [
                { "type": "InitPhone" },
                { "type": "InitMail" },
                { "type": "SetWithdrawBankInfo" }
            ]
        """);

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ILogger<RegisterFlow>)))
			.Returns(NullLogger<RegisterFlow>.Instance);

		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var fakeStartSetPhoneService = Substitute.For<IGetService<StartSetPlayerPhoneCommand, string>>();
		var fakeConfirmSetPhoneService = Substitute.For<ICommandService<CommitInitPlayerPhoneCommand>>();

		_ = fakeServiceProvider.GetService(Arg.Is(flowTypes["InitPhone"]))
			.Returns(new SetPhoneRegisterFlowStep(
				fakeBrandServiceClient,
				fakeStartSetPhoneService,
				fakeConfirmSetPhoneService,
				fakePlayerServiceClient));

		var fakeStartSetMailService = Substitute.For<IGetService<StartSetPlayerEmailCommand, string>>();
		var fakeCommitMailService = Substitute.For<ICommandService<CommitInitPlayerEmailCommand>>();

		_ = fakeServiceProvider.GetService(Arg.Is(flowTypes["InitMail"]))
			.Returns(new SetEmailRegisterFlowStep(
				fakeBrandServiceClient,
				fakeStartSetMailService,
				fakeCommitMailService,
				fakePlayerServiceClient));

		var fakeQueryPlayerBankInfoService = Substitute.For<IQueryService<GetPlayerBankInfosQuery, PlayerBankInfo>>();
		var fakeCreateBankInfoService = Substitute.For<ICommandService<CreateInitBankInfoCommand>>();

		_ = fakeServiceProvider.GetService(Arg.Is(flowTypes["SetWithdrawBankInfo"]))
			.Returns(new SetWithdrawBankInfoRegisterFlowStep(
				fakeQueryPlayerBankInfoService,
				fakeCreateBankInfoService));

		var fakeCreatePlayerService = Substitute.For<IGetService<CreatePlayerCommand, CreatePlayerInfo>>();
		var fakeCreateAgentInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithAgentInvitationCodeCommand, CreatePlayerInfo>>();
		var fakeCreatePlayerInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithPlayerInvitationCommand, CreatePlayerInfo>>();

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(CreatePlayerRegisterFlowStep)))
			.Returns(new CreatePlayerRegisterFlowStep(
				fakeCreatePlayerService,
				fakeCreateAgentInvitationPlayerService,
				fakeCreatePlayerInvitationPlayerService,
				fakePlayerServiceClient,
				fakeBrandServiceClient));

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(FinalRegisterFlowStep)))
			.Returns(new FinalRegisterFlowStep(fakePlayerServiceClient));

		var flowDefinition = sut.LoadSettings(flowSettings);

		var flow = flowDefinition.BuildRegisterFlow();

		var brandId = Guid.NewGuid();
		var inputData = new CreatePlayerInputData
		{
			UserName = "tester",
			Password = "123456"
		};
		var locationInfo = new PlayerLocationInfo(null, "127.0.0.1", null, null);

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			.Returns(new Tgs.BrandCertificate.Clients.BrandInfo
			{
				Id = brandId,
				Currency = "CNY"
			});

		_ = fakeCreatePlayerService.GetAsync(Arg.Is<CreatePlayerCommand>(
			cmd => cmd.UserName == inputData.UserName
				&& cmd.BrandId == brandId))
			.Returns(new CreatePlayerInfo("aabbcc", true));

		var expectedResponse = new CreatePlayerResponse
		{
			IsNew = true,
			PlayerCode = "aabbcc"
		};

		_ = await flow.ConfirmCompletedAsync(null);
		var actual = await flow.ProcessInputAsync(
			brandId,
			JsonSerializer.SerializeToDocument(inputData),
			locationInfo);

		Assert.IsFalse(actual.IsCompleted);
		Assert.AreEqual("InitPhone", actual.NextStepType);
		Assert.AreEqual(expectedResponse, actual.StepResponse.Deserialize<CreatePlayerResponse>());
	}

	[Test]
	public async Task Phone_Mail_BankInfo_Flow_ValidMail_Step()
	{
		var flowTypes = new Dictionary<string, Type>
		{
			["InitPhone"] = typeof(SetPhoneRegisterFlowStep),
			["InitMail"] = typeof(SetEmailRegisterFlowStep),
			["SetWithdrawBankInfo"] = typeof(SetWithdrawBankInfoRegisterFlowStep)
		}.ToImmutableDictionary();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();

		var sut = new RegisterFlowBuilder(fakeServiceProvider, flowTypes);

		var flowSettings = JsonDocument.Parse("""
            [
                { "type": "InitPhone" },
                { "type": "InitMail" },
                { "type": "SetWithdrawBankInfo" }
            ]
        """);

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ILogger<RegisterFlow>)))
			.Returns(NullLogger<RegisterFlow>.Instance);

		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var fakeStartSetPhoneService = Substitute.For<IGetService<StartSetPlayerPhoneCommand, string>>();
		var fakeConfirmSetPhoneService = Substitute.For<ICommandService<CommitInitPlayerPhoneCommand>>();

		_ = fakeServiceProvider.GetService(Arg.Is(flowTypes["InitPhone"]))
			.Returns(new SetPhoneRegisterFlowStep(
				fakeBrandServiceClient,
				fakeStartSetPhoneService,
				fakeConfirmSetPhoneService,
				fakePlayerServiceClient));

		var fakeStartSetMailService = Substitute.For<IGetService<StartSetPlayerEmailCommand, string>>();
		var fakeCommitMailService = Substitute.For<ICommandService<CommitInitPlayerEmailCommand>>();

		_ = fakeServiceProvider.GetService(Arg.Is(flowTypes["InitMail"]))
			.Returns(new SetEmailRegisterFlowStep(
				fakeBrandServiceClient,
				fakeStartSetMailService,
				fakeCommitMailService,
				fakePlayerServiceClient));

		var fakeQueryPlayerBankInfoService = Substitute.For<IQueryService<GetPlayerBankInfosQuery, PlayerBankInfo>>();
		var fakeCreateBankInfoService = Substitute.For<ICommandService<CreateInitBankInfoCommand>>();

		_ = fakeServiceProvider.GetService(Arg.Is(flowTypes["SetWithdrawBankInfo"]))
			.Returns(new SetWithdrawBankInfoRegisterFlowStep(
				fakeQueryPlayerBankInfoService,
				fakeCreateBankInfoService));

		var fakeCreatePlayerService = Substitute.For<IGetService<CreatePlayerCommand, CreatePlayerInfo>>();
		var fakeCreateAgentInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithAgentInvitationCodeCommand, CreatePlayerInfo>>();
		var fakeCreatePlayerInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithPlayerInvitationCommand, CreatePlayerInfo>>();

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(CreatePlayerRegisterFlowStep)))
			.Returns(new CreatePlayerRegisterFlowStep(
				fakeCreatePlayerService,
				fakeCreateAgentInvitationPlayerService,
				fakeCreatePlayerInvitationPlayerService,
				fakePlayerServiceClient,
				fakeBrandServiceClient));

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(FinalRegisterFlowStep)))
			.Returns(new FinalRegisterFlowStep(fakePlayerServiceClient));

		var playerCode = "player1";
		var brandId = Guid.NewGuid();

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(playerCode))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				PlayerCode = playerCode,
				BrandId = brandId,
				PhoneAuthentication = true
			});

		var flowDefinition = sut.LoadSettings(flowSettings);

		var flow = flowDefinition.BuildRegisterFlow();

		var securityKey = Guid.NewGuid().ToString();

		var inputData = new InitEmailStepInputData
		{
			InputType = "Valid",
			Data = JsonSerializer.SerializeToDocument(new ConfirmEmailData
			{
				SecurityKey = securityKey,
				ValidateCode = "123456"
			})
		};
		var locationInfo = new PlayerLocationInfo(playerCode, "127.0.0.1", null, null);

		var expected = Guid.NewGuid().ToString();

		_ = fakeStartSetPhoneService
			.GetAsync(Arg.Is<StartSetPlayerPhoneCommand>(cmd => cmd.Phone == "12345678"
				&& cmd.AreaCode == "123"))
			.Returns(expected);

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			.Returns(new Tgs.BrandCertificate.Clients.BrandInfo
			{
				Id = brandId
			});

		fakeCommitMailService
			.When(fake => fake.ExecuteAsync(
				Arg.Is<CommitInitPlayerEmailCommand>(cmd => cmd.SecurityKey == securityKey)))
			.Do(_ =>
			{
				_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(playerCode))
					.Returns(new Tgs.PlayerService.Client.PlayerInfo
					{
						PlayerCode = playerCode,
						BrandId = brandId,
						PhoneAuthentication = true,
						EmailAuthentication = true
					});
			});

		_ = await flow.ConfirmCompletedAsync(playerCode);
		var actual = await flow.ProcessInputAsync(
			brandId,
			JsonSerializer.SerializeToDocument(inputData),
			locationInfo);

		Assert.IsFalse(actual.IsCompleted);
		Assert.AreEqual("SetWithdrawBankInfo", actual.NextStepType);
	}

	[Test]
	public async Task Phone_Mail_BankInfo_Flow_ValidPhone_Step()
	{
		var flowTypes = new Dictionary<string, Type>
		{
			["InitPhone"] = typeof(SetPhoneRegisterFlowStep),
			["InitMail"] = typeof(SetEmailRegisterFlowStep),
			["SetWithdrawBankInfo"] = typeof(SetWithdrawBankInfoRegisterFlowStep)
		}.ToImmutableDictionary();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();

		var sut = new RegisterFlowBuilder(fakeServiceProvider, flowTypes);

		var flowSettings = JsonDocument.Parse("""
            [
                { "type": "InitPhone" },
                { "type": "InitMail" },
                { "type": "SetWithdrawBankInfo" }
            ]
        """);

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ILogger<RegisterFlow>)))
			.Returns(NullLogger<RegisterFlow>.Instance);

		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var fakeStartSetPhoneService = Substitute.For<IGetService<StartSetPlayerPhoneCommand, string>>();
		var fakeConfirmSetPhoneService = Substitute.For<ICommandService<CommitInitPlayerPhoneCommand>>();

		_ = fakeServiceProvider.GetService(Arg.Is(flowTypes["InitPhone"]))
			.Returns(new SetPhoneRegisterFlowStep(
				fakeBrandServiceClient,
				fakeStartSetPhoneService,
				fakeConfirmSetPhoneService,
				fakePlayerServiceClient));

		var fakeStartSetMailService = Substitute.For<IGetService<StartSetPlayerEmailCommand, string>>();
		var fakeCommitMailService = Substitute.For<ICommandService<CommitInitPlayerEmailCommand>>();

		_ = fakeServiceProvider.GetService(Arg.Is(flowTypes["InitMail"]))
			.Returns(new SetEmailRegisterFlowStep(
				fakeBrandServiceClient,
				fakeStartSetMailService,
				fakeCommitMailService,
				fakePlayerServiceClient));

		var fakeQueryPlayerBankInfoService = Substitute.For<IQueryService<GetPlayerBankInfosQuery, PlayerBankInfo>>();
		var fakeCreateBankInfoService = Substitute.For<ICommandService<CreateInitBankInfoCommand>>();

		_ = fakeServiceProvider.GetService(Arg.Is(flowTypes["SetWithdrawBankInfo"]))
			.Returns(new SetWithdrawBankInfoRegisterFlowStep(
				fakeQueryPlayerBankInfoService,
				fakeCreateBankInfoService));

		var fakeCreatePlayerService = Substitute.For<IGetService<CreatePlayerCommand, CreatePlayerInfo>>();
		var fakeCreateAgentInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithAgentInvitationCodeCommand, CreatePlayerInfo>>();
		var fakeCreatePlayerInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithPlayerInvitationCommand, CreatePlayerInfo>>();

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(CreatePlayerRegisterFlowStep)))
			.Returns(new CreatePlayerRegisterFlowStep(
				fakeCreatePlayerService,
				fakeCreateAgentInvitationPlayerService,
				fakeCreatePlayerInvitationPlayerService,
				fakePlayerServiceClient,
				fakeBrandServiceClient));

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(FinalRegisterFlowStep)))
			.Returns(new FinalRegisterFlowStep(fakePlayerServiceClient));

		var playerCode = "player1";
		var brandId = Guid.NewGuid();

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(playerCode))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				PlayerCode = playerCode,
				BrandId = brandId
			});

		var flowDefinition = sut.LoadSettings(flowSettings);

		var flow = flowDefinition.BuildRegisterFlow();

		var securityKey = Guid.NewGuid().ToString();

		var inputData = new InitPhoneStepInputData
		{
			InputType = "Valid",
			Data = JsonSerializer.SerializeToDocument(new ConfirmPhoneData
			{
				SecurityKey = securityKey,
				ValidateCode = "123456"
			})
		};
		var locationInfo = new PlayerLocationInfo(playerCode, "127.0.0.1", null, null);

		var expected = Guid.NewGuid().ToString();

		_ = fakeStartSetPhoneService
			.GetAsync(Arg.Is<StartSetPlayerPhoneCommand>(cmd => cmd.Phone == "12345678"
				&& cmd.AreaCode == "123"))
			.Returns(expected);

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			.Returns(new Tgs.BrandCertificate.Clients.BrandInfo
			{
				Id = brandId
			});

		fakeConfirmSetPhoneService
			.When(fake => fake.ExecuteAsync(
				Arg.Is<CommitInitPlayerPhoneCommand>(cmd => cmd.SecurityKey == securityKey)))
			.Do(_ =>
			{
				_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(playerCode))
					.Returns(new Tgs.PlayerService.Client.PlayerInfo
					{
						PlayerCode = playerCode,
						BrandId = brandId,
						PhoneAuthentication = true
					});
			});

		_ = await flow.ConfirmCompletedAsync(playerCode);
		var actual = await flow.ProcessInputAsync(
			brandId,
			JsonSerializer.SerializeToDocument(inputData),
			locationInfo);

		Assert.IsFalse(actual.IsCompleted);
		Assert.AreEqual("InitMail", actual.NextStepType);
	}

	[Test]
	public async Task Process_Input_If_Step_Undone_Will_Return_CurrentStepInfo()
	{
		var brandId = Guid.NewGuid();
		var locationInfo = new PlayerLocationInfo("player1", "127.0.0.1", null, null);

		var step1 = new StubRegisterFlowStep()
		{
			Type = "Step1"
		};
		var stepResponse = JsonDocument.Parse("{}");
		step1.SetProcessInputDelegate((_, _, _) => Task.FromResult(
			new RegisterFlowProcessResult(false, null, stepResponse)));
		var step2 = new StubRegisterFlowStep()
		{
			Type = "Step2"
		};
		var step3 = new StubRegisterFlowStep()
		{
			Type = "Step3"
		};

		var sut = new RegisterFlow(new[] {
			step1,
			step2,
			step3
		}, NullLogger<RegisterFlow>.Instance);

		var actual = await sut.ProcessInputAsync(brandId, JsonDocument.Parse("{}"), locationInfo);

		Assert.IsFalse(actual.IsCompleted);
		Assert.AreEqual("Step1", actual.NextStepType);
		Assert.AreEqual(stepResponse, actual.StepResponse);
	}

	[Test]
	public async Task Process_Input_Will_Return_NextStepInfo()
	{
		var brandId = Guid.NewGuid();
		var locationInfo = new PlayerLocationInfo("player1", "127.0.0.1", null, null);

		var step1 = new StubRegisterFlowStep()
		{
			Type = "Step1"
		};
		step1.SetProcessInputDelegate((_, _, _) => Task.FromResult(
			new RegisterFlowProcessResult(true, null, null)));
		var step2 = new StubRegisterFlowStep()
		{
			Type = "Step2"
		};
		var step3 = new StubRegisterFlowStep()
		{
			Type = "Step3"
		};

		var sut = new RegisterFlow(new[] {
			step1,
			step2,
			step3
		}, NullLogger<RegisterFlow>.Instance);

		var actual = await sut.ProcessInputAsync(brandId, JsonDocument.Parse("{}"), locationInfo);

		Assert.IsFalse(actual.IsCompleted);
		Assert.AreEqual("Step2", actual.NextStepType);
	}

	[Test]
	public async Task RegisterFlow_Can_Show_Current_Step()
	{
		var playerCode = "player1";

		var step1 = new StubRegisterFlowStep
		{
			Type = "Step1"
		};
		step1.SetConfirmCompletedDelegate(_ => true);

		var step2 = new StubRegisterFlowStep
		{
			Type = "Step2"
		};
		var step3 = new StubRegisterFlowStep
		{
			Type = "Step3"
		};

		var sut = new RegisterFlow(new[] {
			step1,
			step2,
			step3
		}, NullLogger<RegisterFlow>.Instance);

		var actual = await sut.ConfirmCompletedAsync(playerCode);

		var privateStack = typeof(RegisterFlow)
			.GetField("_flowSteps", BindingFlags.NonPublic | BindingFlags.Instance)
			.GetValue(sut) as Queue<IRegisterFlowStep>;

		Assert.IsFalse(actual);
		Assert.AreEqual("Step2", sut.CurrentStep);
	}

	[Test]
	public async Task RegisterFlow_Will_Auto_Process_FinalRegisterFlowStep()
	{
		var brandId = Guid.NewGuid();
		var locationInfo = new PlayerLocationInfo("player1", "127.0.0.1", null, null);

		var step1 = new StubRegisterFlowStep()
		{
			Type = "Step1"
		};
		var stepResponse = JsonDocument.Parse("{}");
		step1.SetProcessInputDelegate((_, _, _) => Task.FromResult(
			new RegisterFlowProcessResult(true, null, stepResponse)));

		var finalStep = new StubRegisterFlowStep
		{
			Type = "Final"
		};

		finalStep.SetProcessInputDelegate((_, _, _) => Task.FromResult(
			new RegisterFlowProcessResult(true, null, stepResponse)));

		var sut = new RegisterFlow(new[] {
			step1,
			finalStep
		}, NullLogger<RegisterFlow>.Instance);

		var actual = await sut.ProcessInputAsync(brandId, JsonDocument.Parse("{}"), locationInfo);

		Assert.IsTrue(actual.IsCompleted);
		Assert.IsNull(actual.NextStepType);
	}

	[Test]
	public async Task Set_Phone_Without_Check_Duplicate_Step()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new SetPhoneDontCareDuplicateRegisterFlowStep(fakePlayerServiceClient);

		var brandId = Guid.NewGuid();
		var inputData = new PhoneData
		{
			AreaCode = "123",
			Phone = "1345678"
		};

		var locationInfo = new PlayerLocationInfo("player1", "127.0.0.1", null, null);

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(locationInfo.PlayerCode))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				PlayerCode = locationInfo.PlayerCode,
				PhoneAuthentication = true
			});

		var actual = await sut.ProcessInputAsync(
			brandId,
			JsonSerializer.SerializeToDocument(inputData),
			locationInfo);

		Assert.IsTrue(actual.IsCompleted);
		_ = fakePlayerServiceClient.Received(1)
			.SetPlayerPropertiesAsync(
				Arg.Is(locationInfo.PlayerCode),
				Arg.Is<IImmutableDictionary<PlayerPropertyName, string>>(
					dict => dict[PlayerPropertyName.AreaCode] == inputData.AreaCode
						&& dict[PlayerPropertyName.Phone] == inputData.Phone
						&& dict[PlayerPropertyName.PhoneAuthentication] == true.ToString()));
	}

	[Test]
	public async Task Set_Promotion_Join_Step()
	{
		var fakePromotionClient = Substitute.For<IPromotionClient>();
		var fakeLogger = new NullLogger<SetPromotionJoinRegisterFlowStep>();

		var sut = new SetPromotionJoinRegisterFlowStep(fakePromotionClient, fakeLogger);

		var brandId = Guid.NewGuid();
		var inputData = new Guid[]
		{
			Guid.Parse("ca46f83c-8301-0000-0001-00007afd9fad")
		};

		var jsonString = """
			[{
				"type": "JoinPromotion",
				"promotionIds": ["ca46f83c-8301-0000-0001-00007afd9fad"]
			}]
			""";
		var settingsJson = JsonDocument.Parse(jsonString);

		var element = settingsJson.RootElement.EnumerateArray().First();

		sut.SetupStep(element);

		var locationInfo = new PlayerLocationInfo("player1", "127.0.0.1", null, null);

		var actual = await sut.ProcessInputAsync(
			brandId,
			JsonSerializer.SerializeToDocument(inputData),
			locationInfo);

		Assert.IsTrue(actual.IsCompleted);
	}

	[Test]
	public async Task SetEmailDontCareDuplicateRegisterFlowStep_ProcessInput()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeCacheManager = Substitute.For<IRedisCacheManager>();

		var sut = new SetEmailDontCareDuplicateRegisterFlowStep(
			fakePlayerServiceClient,
			fakeCacheManager);

		var brandId = Guid.NewGuid();
		var inputData = new EmailData
		{
			Email = "tester@test.com"
		};
		var locationInfo = new PlayerLocationInfo("player1", "127.0.0.1", null, null);

		fakePlayerServiceClient
			.When(fake => fake
				.SetPlayerPropertiesAsync(
					Arg.Is(locationInfo.PlayerCode),
					Arg.Any<IImmutableDictionary<PlayerPropertyName, string>>()))
			.Do(callInfo =>
			{
				var dict = callInfo.Arg<IImmutableDictionary<PlayerPropertyName, string>>();

				if (dict[PlayerPropertyName.Email] == inputData.Email)
				{
					_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(locationInfo.PlayerCode))
						.Returns(new Tgs.PlayerService.Client.PlayerInfo
						{
							EmailAuthentication = true
						});
				}
			});

		var actual = await sut.ProcessInputAsync(brandId, JsonSerializer.SerializeToDocument(inputData), locationInfo);

		Assert.IsTrue(actual.IsCompleted);
	}

	[Test]
	public async Task SetEmailRegisterFlowStep_ConfirmCompleted_With_Player_Already_Authenticate_Email_Should_Return_True()
	{
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeStartSetMailService = Substitute.For<IGetService<StartSetPlayerEmailCommand, string>>();
		var fakeCommitMailService = Substitute.For<ICommandService<CommitInitPlayerEmailCommand>>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new SetEmailRegisterFlowStep(
			fakeBrandServiceClient,
			fakeStartSetMailService,
			fakeCommitMailService,
			fakePlayerServiceClient);

		var playerCode = "player1";

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(playerCode))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				PlayerCode = "tester",
				EmailAuthentication = true
			});

		var actual = await sut.ConfirmCompletedAsync(playerCode);

		Assert.IsTrue(actual);
	}

	[Test]
	public async Task SetEmailRegisterFlowStep_ProcessInitEmail()
	{
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeStartSetMailService = Substitute.For<IGetService<StartSetPlayerEmailCommand, string>>();
		var fakeCommitMailService = Substitute.For<ICommandService<CommitInitPlayerEmailCommand>>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new SetEmailRegisterFlowStep(
			fakeBrandServiceClient,
			fakeStartSetMailService,
			fakeCommitMailService,
			fakePlayerServiceClient);

		var brandId = Guid.NewGuid();
		var emailData = new EmailData
		{
			Email = "tester@test.com"
		};
		var inputData = new InitEmailStepInputData
		{
			InputType = "Init",
			Data = JsonSerializer.SerializeToDocument(emailData)
		};
		var locationInfo = new PlayerLocationInfo("player1", "127.0.0.1", null, null);

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			.Returns(new Tgs.BrandCertificate.Clients.BrandInfo
			{
				Id = brandId
			});

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(locationInfo.PlayerCode))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				PlayerCode = locationInfo.PlayerCode
			});

		var actual = await sut.ProcessInputAsync(
			brandId,
			JsonSerializer.SerializeToDocument(inputData),
			locationInfo);

		Assert.IsFalse(actual.IsCompleted);
		Assert.NotNull(actual.StepResponse);
	}

	[Test]
	public async Task SetEmailRegisterFlowStep_ProcessValidEmail()
	{
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeStartSetMailService = Substitute.For<IGetService<StartSetPlayerEmailCommand, string>>();
		var fakeCommitMailService = Substitute.For<ICommandService<CommitInitPlayerEmailCommand>>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new SetEmailRegisterFlowStep(
			fakeBrandServiceClient,
			fakeStartSetMailService,
			fakeCommitMailService,
			fakePlayerServiceClient);

		var brandId = Guid.NewGuid();
		var confirmData = new ConfirmEmailData
		{
			SecurityKey = Guid.NewGuid().ToString(),
			ValidateCode = "123456"
		};
		var inputData = new InitEmailStepInputData
		{
			InputType = "Valid",
			Data = JsonSerializer.SerializeToDocument(confirmData)
		};
		var locationInfo = new PlayerLocationInfo("player1", "127.0.0.1", null, null);

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(locationInfo.PlayerCode))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				PlayerCode = locationInfo.PlayerCode
			});

		fakeCommitMailService
			.When(fake => fake.ExecuteAsync(Arg.Any<CommitInitPlayerEmailCommand>()))
			.Do(_ =>
			{
				_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(locationInfo.PlayerCode))
					.Returns(new Tgs.PlayerService.Client.PlayerInfo
					{
						PlayerCode = locationInfo.PlayerCode,
						EmailAuthentication = true
					});
			});

		var actual = await sut.ProcessInputAsync(
			brandId,
			JsonSerializer.SerializeToDocument(inputData),
			locationInfo);

		Assert.IsTrue(actual.IsCompleted);
	}

	[Test]
	public async Task SetPhoneDontCareDuplicateRegisterFlow_ProcessInput()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new SetPhoneDontCareDuplicateRegisterFlowStep(fakePlayerServiceClient);

		var brandId = Guid.NewGuid();
		var phoneData = new PhoneData
		{
			AreaCode = "123",
			Phone = "1345678"
		};
		var locationInfo = new PlayerLocationInfo("player1", "127.0.0.1", null, null);

		fakePlayerServiceClient
			.When(fake => fake
				.SetPlayerPropertiesAsync(
					Arg.Is(locationInfo.PlayerCode),
					Arg.Any<IImmutableDictionary<PlayerPropertyName, string>>()))
			.Do(callInfo =>
			{
				var dict = callInfo.Arg<IImmutableDictionary<PlayerPropertyName, string>>();

				if (dict[PlayerPropertyName.AreaCode] == phoneData.AreaCode
					&& dict[PlayerPropertyName.Phone] == phoneData.Phone
					&& dict[PlayerPropertyName.PhoneAuthentication] == true.ToString())
				{
					_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(locationInfo.PlayerCode))
						.Returns(new Tgs.PlayerService.Client.PlayerInfo
						{
							PhoneAuthentication = true
						});
				}
			});

		var actual = await sut.ProcessInputAsync(brandId, JsonSerializer.SerializeToDocument(phoneData), locationInfo);

		Assert.IsTrue(actual.IsCompleted);
	}

	[Test]
	public async Task SetPhoneRegisterFlowStep_ConfirmCompleted_With_Player_Already_Authenticate_Phone_Should_Return_True()
	{
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeStartSetPhoneService = Substitute.For<IGetService<StartSetPlayerPhoneCommand, string>>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeConfirmSetPhoneService = Substitute.For<ICommandService<CommitInitPlayerPhoneCommand>>();

		var sut = new SetPhoneRegisterFlowStep(
			fakeBrandServiceClient,
			fakeStartSetPhoneService,
			fakeConfirmSetPhoneService,
			fakePlayerServiceClient);

		var playerCode = "player1";

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(playerCode))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				PlayerCode = "tester",
				PhoneAuthentication = true
			});

		var actual = await sut.ConfirmCompletedAsync(playerCode);

		Assert.IsTrue(actual);
	}

	[Test]
	public async Task SetPhoneRegisterFlowStep_ProcessInitPhone()
	{
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeStartSetPhoneService = Substitute.For<IGetService<StartSetPlayerPhoneCommand, string>>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeConfirmSetPhoneService = Substitute.For<ICommandService<CommitInitPlayerPhoneCommand>>();

		var sut = new SetPhoneRegisterFlowStep(
			fakeBrandServiceClient,
			fakeStartSetPhoneService,
			fakeConfirmSetPhoneService,
			fakePlayerServiceClient);

		var brandId = Guid.NewGuid();
		var phoneData = new PhoneData
		{
			AreaCode = "123",
			Phone = "1345678"
		};
		var inputData = new InitPhoneStepInputData
		{
			InputType = "Init",
			Data = JsonSerializer.SerializeToDocument(phoneData)
		};
		var locationInfo = new PlayerLocationInfo("player1", "127.0.0.1", null, null);

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
			.Returns(new Tgs.BrandCertificate.Clients.BrandInfo
			{
				Id = brandId,
				Code = "Test",
				Currency = "CNY",
				SmsRegisterFlowEnabled = true
			});

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(locationInfo.PlayerCode))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				PlayerCode = locationInfo.PlayerCode
			});

		var actual = await sut.ProcessInputAsync(
			brandId,
			JsonSerializer.SerializeToDocument(inputData),
			locationInfo);

		Assert.IsFalse(actual.IsCompleted);
		Assert.NotNull(actual.StepResponse);
	}

	[Test]
	public async Task SetPhoneRegisterFlowStep_ProcessValidPhone()
	{
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeStartSetPhoneService = Substitute.For<IGetService<StartSetPlayerPhoneCommand, string>>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeConfirmSetPhoneService = Substitute.For<ICommandService<CommitInitPlayerPhoneCommand>>();

		var sut = new SetPhoneRegisterFlowStep(
			fakeBrandServiceClient,
			fakeStartSetPhoneService,
			fakeConfirmSetPhoneService,
			fakePlayerServiceClient);

		var brandId = Guid.NewGuid();
		var confirmData = new ConfirmPhoneData
		{
			SecurityKey = Guid.NewGuid().ToString(),
			ValidateCode = "123456"
		};
		var inputData = new InitPhoneStepInputData
		{
			InputType = "Valid",
			Data = JsonSerializer.SerializeToDocument(confirmData)
		};
		var locationInfo = new PlayerLocationInfo("player1", "127.0.0.1", null, null);

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(locationInfo.PlayerCode))
			.Returns(new Tgs.PlayerService.Client.PlayerInfo
			{
				PlayerCode = locationInfo.PlayerCode,
				PhoneAuthentication = true
			});

		var actual = await sut.ProcessInputAsync(
			brandId,
			JsonSerializer.SerializeToDocument(inputData),
			locationInfo);

		Assert.IsTrue(actual.IsCompleted);
	}

	[Test]
	public async Task SetRealNameRegisterFlowStep_ProcessInput()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new SetRealNameRegisterFlowStep(fakePlayerServiceClient);

		var brandId = Guid.NewGuid();
		var inputData = new SetRealNameRegisterFlowStep.RealNameData
		{
			RealName = "RealName"
		};
		var locationInfo = new PlayerLocationInfo("player1", "127.0.0.1", null, null);

		fakePlayerServiceClient
		   .When(fake => fake
			   .SetPlayerPropertiesAsync(
				   Arg.Is(locationInfo.PlayerCode),
				   Arg.Any<IImmutableDictionary<PlayerPropertyName, string>>()))
		   .Do(callInfo =>
		   {
			   var dict = callInfo.Arg<IImmutableDictionary<PlayerPropertyName, string>>();

			   if (dict[PlayerPropertyName.RealName] == inputData.RealName)
			   {
				   _ = fakePlayerServiceClient.GetPlayerPropertiesAsync(Arg.Is(locationInfo.PlayerCode))
					   .Returns(new Dictionary<string, string>
					   {
						   [nameof(PlayerPropertyName.RealName)] = inputData.RealName
					   }.ToImmutableDictionary());
			   }
		   });

		var actual = await sut.ProcessInputAsync(
			brandId,
			JsonSerializer.SerializeToDocument(inputData),
			locationInfo);

		Assert.IsTrue(actual.IsCompleted);
	}

	[Test]
	public async Task SetWithdrawBankInfoRegisterStep_If_Has_Withdraw_BankInfo_ConfirmCompleted_Should_Return_True()
	{
		var fakeQueryPlayerBankInfoService = Substitute.For<IQueryService<GetPlayerBankInfosQuery, PlayerBankInfo>>();
		var fakeCreateBankInfoService = Substitute.For<ICommandService<CreateInitBankInfoCommand>>();

		var sut = new SetWithdrawBankInfoRegisterFlowStep(
			fakeQueryPlayerBankInfoService,
			fakeCreateBankInfoService);

		var playerCode = "player1";
		var brandId = Guid.NewGuid();

		_ = fakeQueryPlayerBankInfoService.QueryAsync(Arg.Any<GetPlayerBankInfosQuery>())
			.Returns(new[] {
				new PlayerBankInfo
				{
					BankType = "Bank",
					BrandId = brandId,
					Id = Guid.NewGuid(),
					PaymentChannelId = Guid.NewGuid(),
					PlayerCode = playerCode,
					IsDefault=true
				}
			}.ToAsyncEnumerable());

		var actual = await sut.ConfirmCompletedAsync(playerCode);

		Assert.IsTrue(actual);
	}

	[Test]
	public async Task SetWithdrawBankInfoRegisterStep_ProcssInput()
	{
		var fakeQueryPlayerBankInfoService = Substitute.For<IQueryService<GetPlayerBankInfosQuery, PlayerBankInfo>>();
		var fakeCreateBankInfoService = Substitute.For<ICommandService<CreateInitBankInfoCommand>>();

		var sut = new SetWithdrawBankInfoRegisterFlowStep(
			fakeQueryPlayerBankInfoService,
			fakeCreateBankInfoService);

		var brandId = Guid.NewGuid();
		var inputData = new BankInfoData
		{
			BankType = "Bank",
			Detail = JsonDocument.Parse("{}"),
			PaymentChannelId = Guid.NewGuid()
		};

		var locationInfo = new PlayerLocationInfo(
			"player1",
			"127.0.0.1",
			null,
			null);

		var actual = await sut.ProcessInputAsync(
			brandId,
			JsonSerializer.SerializeToDocument(inputData),
			locationInfo);

		Assert.IsTrue(actual.IsCompleted);
	}

	[Test]
	public async Task Undone_Flow_Call_ConfirmCompleted_Should_Return_False()
	{
		var playerCode = "player1";

		var sut = new RegisterFlow(new[] {
			new StubRegisterFlowStep()
		}, NullLogger<RegisterFlow>.Instance);

		var actual = await sut.ConfirmCompletedAsync(playerCode);

		Assert.IsFalse(actual);
	}
}
