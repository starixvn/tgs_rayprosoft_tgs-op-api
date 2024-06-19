using Microsoft.AspNetCore.Http;
using NSubstitute;
using Tgs.Agent.Client;
using Tgs.Agent.Client.Models;
using Tgs.Agent.Client.Models.Enums;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Controllers;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Models.Tokens;
using TGS.Services.Operator.ApplicationServices.Player;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class AgentControllerTests
{
	[Test]
	public async Task Get_Direct_Agents_By_Agent_Username()
	{
		var fakeHttpContext = Substitute.For<HttpContext>();
		var fakePlayerAgentService = Substitute.For<IGetService<GetPlayerAgentQuery, Guid?>>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeAgentClient = Substitute.For<IAgentClient>();

		var playerTokenData = new PlayerTokenData { PlayerCode = "playerCode" };

		var brandId = Guid.NewGuid();
		var agentId = Guid.NewGuid();

		var childAgentInfos = new[]
		{
			new ChildAgentInfoModel { AgentUsername = "test", AgentId = Guid.NewGuid(), ParentAgentId = agentId }, 
			new ChildAgentInfoModel { AgentUsername = "TEST", AgentId = Guid.NewGuid(), ParentAgentId = agentId },
			new ChildAgentInfoModel { AgentUsername = "ABCDE", AgentId = Guid.NewGuid(), ParentAgentId = Guid.NewGuid() }
		};

		var items = new Dictionary<object, object?> { { "brandId", brandId }, { "playerToken", playerTokenData } };

		_ = fakeHttpContext.Items.Returns(items);

		_ = fakePlayerAgentService.GetAsync(Arg.Any<GetPlayerAgentQuery>(), Arg.Any<CancellationToken>())
			.Returns(agentId);

		_ = fakeAgentClient.GetChildAgentInfosAsync(Arg.Any<GetChildAgentInfosRequest>(), Arg.Any<CancellationToken>())
			.Returns(childAgentInfos.ToAsyncEnumerable());

		var sut = new AgentController() { ControllerContext = { HttpContext = fakeHttpContext } };

		var query = new GetDirectAgentsRequest { AgentUsername = "test", Rows = 10, Page = 1 };

		var response =
			await sut.GetDirectAgentsAsync(fakePlayerAgentService, fakePlayerServiceClient, fakeAgentClient, query);

		Assert.That(response.Items.Length, Is.EqualTo(2));
	}

	[Test]
	public async Task Get_Direct_Agents_By_Agent_Username2()
	{
		var fakeHttpContext = Substitute.For<HttpContext>();
		var fakePlayerAgentService = Substitute.For<IGetService<GetPlayerAgentQuery, Guid?>>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeAgentClient = Substitute.For<IAgentClient>();

		var playerTokenData = new PlayerTokenData { PlayerCode = "playerCode" };

		var brandId = Guid.NewGuid();
		var agentId = Guid.NewGuid();

		var childAgentInfos = new[]
		{
			new ChildAgentInfoModel { AgentUsername = "test", AgentId = Guid.NewGuid(), ParentAgentId = agentId }, 
			new ChildAgentInfoModel { AgentUsername = "TEST", AgentId = Guid.NewGuid(), ParentAgentId = agentId },
			new ChildAgentInfoModel { AgentUsername = "ABCDE", AgentId = Guid.NewGuid(), ParentAgentId = agentId }
		};

		var items = new Dictionary<object, object?> { { "brandId", brandId }, { "playerToken", playerTokenData } };

		_ = fakeHttpContext.Items.Returns(items);

		_ = fakePlayerAgentService.GetAsync(Arg.Any<GetPlayerAgentQuery>(), Arg.Any<CancellationToken>())
			.Returns(agentId);

		_ = fakeAgentClient.GetChildAgentInfosAsync(Arg.Any<GetChildAgentInfosRequest>(), Arg.Any<CancellationToken>())
			.Returns(childAgentInfos.ToAsyncEnumerable());

		var sut = new AgentController() { ControllerContext = { HttpContext = fakeHttpContext } };

		var query = new GetDirectAgentsRequest { AgentUsername = "cd", Rows = 10, Page = 1 };

		var response =
			await sut.GetDirectAgentsAsync(fakePlayerAgentService, fakePlayerServiceClient, fakeAgentClient, query);

		Assert.That(response.Items.Length, Is.EqualTo(1));
	}

	[Test]
	public async Task Get_Direct_Agents_By_Agent_Username_And_Status()
	{
		var fakeHttpContext = Substitute.For<HttpContext>();
		var fakePlayerAgentService = Substitute.For<IGetService<GetPlayerAgentQuery, Guid?>>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeAgentClient = Substitute.For<IAgentClient>();

		var playerTokenData = new PlayerTokenData { PlayerCode = "playerCode" };

		var brandId = Guid.NewGuid();
		var agentId = Guid.NewGuid();

		var childAgentInfos = new[]
		{
			new ChildAgentInfoModel { AgentUsername = "test", Status = AgentStatus.Enabled, AgentId = Guid.NewGuid(), ParentAgentId = agentId },
			new ChildAgentInfoModel { AgentUsername = "TEST", Status = AgentStatus.Disabled, AgentId = Guid.NewGuid(), ParentAgentId = agentId },
			new ChildAgentInfoModel { AgentUsername = "ABCDE", Status = AgentStatus.Enabled, AgentId = Guid.NewGuid(), ParentAgentId = Guid.NewGuid() }
		};

		var items = new Dictionary<object, object?> { { "brandId", brandId }, { "playerToken", playerTokenData } };

		_ = fakeHttpContext.Items.Returns(items);

		_ = fakePlayerAgentService.GetAsync(Arg.Any<GetPlayerAgentQuery>(), Arg.Any<CancellationToken>())
			.Returns(agentId);

		_ = fakeAgentClient.GetChildAgentInfosAsync(Arg.Any<GetChildAgentInfosRequest>(), Arg.Any<CancellationToken>())
			.Returns(childAgentInfos.ToAsyncEnumerable());

		var sut = new AgentController() { ControllerContext = { HttpContext = fakeHttpContext } };

		var query = new GetDirectAgentsRequest { AgentUsername = "es", AgentStatus = Core.Models.Enums.AgentStatus.Enabled, Rows = 10, Page = 1 };

		var response =
			await sut.GetDirectAgentsAsync(fakePlayerAgentService, fakePlayerServiceClient, fakeAgentClient, query);

		Assert.That(response.Items.Length, Is.EqualTo(1));
	}

	[TestCase(Core.Models.Enums.AgentStatus.Enabled, 2)]
	[TestCase(Core.Models.Enums.AgentStatus.Disabled, 1)]
	public async Task Get_Direct_Agents_By_Status(Core.Models.Enums.AgentStatus status, int count)
	{
		var fakeHttpContext = Substitute.For<HttpContext>();
		var fakePlayerAgentService = Substitute.For<IGetService<GetPlayerAgentQuery, Guid?>>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeAgentClient = Substitute.For<IAgentClient>();

		var playerTokenData = new PlayerTokenData { PlayerCode = "playerCode" };

		var brandId = Guid.NewGuid();
		var agentId = Guid.NewGuid();

		var childAgentInfos = new[]
		{
			new ChildAgentInfoModel { AgentUsername = "test", Status = AgentStatus.Enabled, AgentId = Guid.NewGuid(), ParentAgentId = agentId },
			new ChildAgentInfoModel { AgentUsername = "TEST", Status = AgentStatus.Disabled, AgentId = Guid.NewGuid(), ParentAgentId = agentId },
			new ChildAgentInfoModel { AgentUsername = "ABCDE", Status = AgentStatus.Enabled, AgentId = Guid.NewGuid(), ParentAgentId = agentId }
		};

		var items = new Dictionary<object, object?> { { "brandId", brandId }, { "playerToken", playerTokenData } };

		_ = fakeHttpContext.Items.Returns(items);

		_ = fakePlayerAgentService.GetAsync(Arg.Any<GetPlayerAgentQuery>(), Arg.Any<CancellationToken>())
			.Returns(agentId);

		_ = fakeAgentClient.GetChildAgentInfosAsync(Arg.Any<GetChildAgentInfosRequest>(), Arg.Any<CancellationToken>())
			.Returns(childAgentInfos.ToAsyncEnumerable());

		var sut = new AgentController() { ControllerContext = { HttpContext = fakeHttpContext } };

		var query = new GetDirectAgentsRequest { AgentUsername = null, AgentStatus = status, Rows = 10, Page = 1 };

		var response =
			await sut.GetDirectAgentsAsync(fakePlayerAgentService, fakePlayerServiceClient, fakeAgentClient, query);

		Assert.That(response.Items.Length, Is.EqualTo(count));
	}

	[Test]
	public async Task Get_Direct_Agents()
	{
		var fakeHttpContext = Substitute.For<HttpContext>();
		var fakePlayerAgentService = Substitute.For<IGetService<GetPlayerAgentQuery, Guid?>>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeAgentClient = Substitute.For<IAgentClient>();

		var playerTokenData = new PlayerTokenData { PlayerCode = "playerCode" };

		var brandId = Guid.NewGuid();
		var agentId = Guid.NewGuid();
		var agentId2 = Guid.NewGuid();
		var agentId3 = Guid.NewGuid();
		var agentId4 = Guid.NewGuid();
		var agentId5 = Guid.NewGuid();
		var agentId6 = Guid.NewGuid();

		var childAgentInfos = new[]
		{
			new ChildAgentInfoModel { AgentUsername = "test", AgentId = agentId6, ParentAgentId = agentId5}, 
			new ChildAgentInfoModel { AgentUsername = "TEST", AgentId = agentId2, ParentAgentId = agentId },
			new ChildAgentInfoModel { AgentUsername = "ABCDE", AgentId = agentId3, ParentAgentId = agentId2 },
			new ChildAgentInfoModel { AgentUsername = "ABCDE22", AgentId = agentId4, ParentAgentId = agentId3 },
			new ChildAgentInfoModel { AgentUsername = "ABCDEss", AgentId = agentId5, ParentAgentId = agentId4 }
		};

		var items = new Dictionary<object, object?> { { "brandId", brandId }, { "playerToken", playerTokenData } };

		var playerCountInfos = new PlayerCountInfo[]
		{
			new PlayerCountInfo(agentId2, 10), new PlayerCountInfo(null, 10)
		}.ToAsyncEnumerable();

		_ = fakeHttpContext.Items.Returns(items);

		_ = fakePlayerAgentService.GetAsync(Arg.Any<GetPlayerAgentQuery>(), Arg.Any<CancellationToken>())
			.Returns(agentId);

		_ = fakeAgentClient.GetChildAgentInfosAsync(Arg.Any<GetChildAgentInfosRequest>(), Arg.Any<CancellationToken>())
			.Returns(childAgentInfos.ToAsyncEnumerable());

		_ = fakePlayerServiceClient.GetPlayerCountInfosByBrandIdAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(playerCountInfos);

		var sut = new AgentController() { ControllerContext = { HttpContext = fakeHttpContext } };

		var query = new GetDirectAgentsRequest { Rows = 10, Page = 1 };

		var response =
			await sut.GetDirectAgentsAsync(fakePlayerAgentService, fakePlayerServiceClient, fakeAgentClient, query);

		Assert.That(response.Items.Length, Is.EqualTo(1));
	}
}