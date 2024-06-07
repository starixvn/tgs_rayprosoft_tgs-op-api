using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Tgs.Agent.Dividend.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Agent;

internal class GetNegativeAgentDividendQueryService(IAgentDividendClient agentDividendClient)
	: IGetService<GetNegativeAgentDividendQuery, decimal>
{
	public async ValueTask<decimal> GetAsync(
		GetNegativeAgentDividendQuery query,
		CancellationToken cancellationToken = default)
	{
		return await agentDividendClient.GetNegativeAgentDividendAsync(query.AgentId,
			cancellationToken);
	}
}