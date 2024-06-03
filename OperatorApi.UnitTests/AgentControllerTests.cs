using Microsoft.AspNetCore.Http;
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
			new ChildAgentInfoModel { AgentUsername = "test" }, new ChildAgentInfoModel { AgentUsername = "TEST" },
			new ChildAgentInfoModel { AgentUsername = "ABCDE" }
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
			new ChildAgentInfoModel { AgentUsername = "test" }, new ChildAgentInfoModel { AgentUsername = "TEST" },
			new ChildAgentInfoModel { AgentUsername = "ABCDE" }
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
			new ChildAgentInfoModel { AgentUsername = "test", Status = AgentStatus.Enabled },
			new ChildAgentInfoModel { AgentUsername = "TEST", Status = AgentStatus.Disabled },
			new ChildAgentInfoModel { AgentUsername = "ABCDE", Status = AgentStatus.Enabled }
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
			new ChildAgentInfoModel { AgentUsername = "test", Status = AgentStatus.Enabled },
			new ChildAgentInfoModel { AgentUsername = "TEST", Status = AgentStatus.Disabled },
			new ChildAgentInfoModel { AgentUsername = "ABCDE", Status = AgentStatus.Enabled }
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
}