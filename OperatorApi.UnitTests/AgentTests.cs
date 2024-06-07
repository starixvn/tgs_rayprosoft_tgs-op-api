using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Tgs.Agent.Client;
using Tgs.Agent.Client.Models;
using Tgs.Agent.Dividend.Client;
using Tgs.PlayerService.Client;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Agent;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class AgentTests
{
	[Test]
	public async Task Can_Get_AgentInfo_By_InvitationCode()
	{
		var fakeAgentRepository = Substitute.For<IAgentRepository>();

		var sut = new GetAgentInfoByInvitationCodeQueryService(fakeAgentRepository);

		var query = new GetAgentInfoByInvitationCodeQuery("test");

		_ = await sut.GetAsync(query);

		_ = fakeAgentRepository.Received(1)
			.GetAgentInfoByInvitationCodeAsync(Arg.Is(query.InvitationCode));
	}

	[Test]
	public async Task Get_Agent_Info()
	{
		var fakeAgentClient = Substitute.For<IAgentClient>();
		var fakeMemoryCache = Substitute.For<IMemoryCache>();

		var sut = new GetAgentInfoQueryService(fakeAgentClient);

		var query = new GetAgentInfoQuery(Guid.NewGuid());

		_ = await sut.GetAsync(query);

		_ = fakeAgentClient.Received(1)
			.GetAgentInfoAsync(Arg.Any<GetAgentInfoRequest>(), Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task Get_Agent_Product_Fee_Info()
	{
		var fakeAgentClient = Substitute.For<IAgentClient>();
		var fakeMemoryCache = Substitute.For<IMemoryCache>();

		var sut = new GetAgentProductFeeInfoQueryService(fakeAgentClient);

		var query = new GetAgentProductFeeInfoQuery(Guid.NewGuid());

		_ = await sut.GetAsync(query);

		_ = fakeAgentClient.Received(1)
			.GetAgentProductFeeAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task Get_Agent_Team_Info_Info()
	{
		var fakeAgentClient = Substitute.For<IAgentClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new GetAgentTeamInfoQueryService(fakeAgentClient, fakePlayerServiceClient);

		var query = new GetAgentTeamInfoQuery(Guid.NewGuid(), Guid.NewGuid());

		_ = await sut.GetAsync(query);

		_ = fakeAgentClient.Received(1)
			.GetChildAgentInfosAsync(Arg.Any<GetChildAgentInfosRequest>(), Arg.Any<CancellationToken>());

		_ = fakePlayerServiceClient.Received(1)
			.GetPlayerCountInfosByBrandIdAsync(Arg.Any<Guid>(), Arg.Is(false), Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task Get_Negative_Agent_Dividend()
	{
		var fakeAgentDividendClient = Substitute.For<IAgentDividendClient>();
		var fakeMemoryCache = Substitute.For<IMemoryCache>();

		var sut = new GetNegativeAgentDividendQueryService(fakeAgentDividendClient);

		var query = new GetNegativeAgentDividendQuery(Guid.NewGuid());

		_ = await sut.GetAsync(query);

		_ = fakeAgentDividendClient.Received(1)
			.GetNegativeAgentDividendAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
	}

}