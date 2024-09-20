using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class GetPlayerAgentQueryService(
	IPlayerAgentRepository playerAgentRepository) : IGetService<GetPlayerAgentQuery, Guid?>
{
	public async ValueTask<Guid?> GetAsync(
		GetPlayerAgentQuery query,
		CancellationToken cancellationToken = default)
	{
		return await playerAgentRepository.GetPlayerAgentIdAsync(query.PlayerCode, cancellationToken);
	}
}