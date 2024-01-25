using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Agent;

internal class GetAgentInfoByInvitationCodeQueryService(IAgentRepository agentRepository) : IGetService<GetAgentInfoByInvitationCodeQuery, AgentInfo?>
{
	private readonly IAgentRepository _agentRepository = agentRepository ?? throw new System.ArgumentNullException(nameof(agentRepository));

	public ValueTask<AgentInfo?> GetAsync(
		GetAgentInfoByInvitationCodeQuery query,
		CancellationToken cancellationToken = default)
		=> _agentRepository.GetAgentInfoByInvitationCodeAsync(query.InvitationCode, cancellationToken);
}
