using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Tgs.Agent.Client;
using Tgs.Agent.Client.Models;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Agent;

internal class GetAgentProductFeeInfoQueryService(IAgentClient agentClient, IMemoryCache memoryCache)
	: IGetService<GetAgentProductFeeInfoQuery, AgentProductFeeInfo?>
{
	public async ValueTask<AgentProductFeeInfo?> GetAsync(
		GetAgentProductFeeInfoQuery query,
		CancellationToken cancellationToken = default)
	{
		var key = $"AgentProductFeeInfo:{query.AgentId}";

		return await memoryCache.GetOrCreateAsync(
				key,
				async entry =>
				{
					entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5D);

					return await agentClient.GetAgentProductFeeAsync(query.AgentId,
						cancellationToken);
				})
			.ConfigureAwait(false);
	}
}