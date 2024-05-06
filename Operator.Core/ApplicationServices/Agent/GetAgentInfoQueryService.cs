using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Tgs.Agent.Client;
using Tgs.Agent.Client.Models;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Agent;

internal class GetAgentInfoQueryService(IAgentClient agentClient, IMemoryCache memoryCache)
	: IGetService<GetAgentInfoQuery, AgentInfoModel?>
{
	public async ValueTask<AgentInfoModel?> GetAsync(
		GetAgentInfoQuery query,
		CancellationToken cancellationToken = default)
	{
		var key = $"AgentInfoModel:{query.AgentId}";

		return await memoryCache.GetOrCreateAsync(
				key,
				async entry =>
				{
					entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5D);

					return await agentClient.GetAgentInfoAsync(new GetAgentInfoRequest(query.AgentId),
						cancellationToken);
				})
			.ConfigureAwait(false);
	}
}