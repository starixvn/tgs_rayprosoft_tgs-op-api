using NSubstitute;
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
}
