using System.Threading;
using System.Threading.Tasks;
using Tgs.Player.All.Agent.Client;
using Tgs.Player.All.Agent.Client.Models;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.BrandAllAgent;

internal class GetBrandAllAgentSwitchOnQueryService(IBrandAllAgentClient client) : IGetService<GetBrandAllAgentSwitchOnQuery, bool>
{
	public async ValueTask<bool> GetAsync(
		GetBrandAllAgentSwitchOnQuery query,
		CancellationToken cancellationToken = default)
		=> (await client.GetBrandAllAgentAsync(new GetBrandAllAgentRequest(query.BrandId))).Switch;
}