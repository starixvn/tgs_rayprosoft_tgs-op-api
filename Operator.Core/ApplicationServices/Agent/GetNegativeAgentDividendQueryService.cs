using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Tgs.Agent.Dividend.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Agent;

internal class GetNegativeAgentDividendQueryService(IAgentDividendClient agentDividendClient, IMemoryCache memoryCache)
	: IGetService<GetNegativeAgentDividendQuery, decimal>
{
	public async ValueTask<decimal> GetAsync(
		GetNegativeAgentDividendQuery query,
		CancellationToken cancellationToken = default)
	{
		var key = $"NegativeAgentDividend:{query.AgentId}";

		return await memoryCache.GetOrCreateAsync(
				key,
				async entry =>
				{
					entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5D);

					return await agentDividendClient.GetNegativeAgentDividendAsync(query.AgentId,
						cancellationToken);
				})
			.ConfigureAwait(false);
	}
}