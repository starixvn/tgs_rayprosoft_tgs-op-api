using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Tgs.Agent.Client;
using Tgs.Agent.Client.Models;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Agent;

internal class GetAgentInfoQueryService(IAgentClient agentClient)
	: IGetService<GetAgentInfoQuery, AgentInfoModel?>
{
	public async ValueTask<AgentInfoModel?> GetAsync(
		GetAgentInfoQuery query,
		CancellationToken cancellationToken = default)
	{
		return await agentClient.GetAgentInfoAsync(new GetAgentInfoRequest(query.AgentId),
			cancellationToken);
	}
}