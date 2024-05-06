using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Services.Operator.PlayerRegisterFlow;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class GetPlayerAgentQueryQueryService(
	IPlayerServiceClient playerServiceClient,
	IMemoryCache memoryCache) : IGetService<GetPlayerAgentQuery, Guid?>
{
	public async ValueTask<Guid?> GetAsync(
		GetPlayerAgentQuery query,
		CancellationToken cancellationToken = default)
	{
		var key = $"PlayerAgentId:{query.PlayerCode}";

		return await memoryCache.GetOrCreateAsync(
				key,
				async entry =>
				{
					entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5D);

					return await playerServiceClient.GetAgentIdForPlayerUpgradedAsync(query.PlayerCode,
						cancellationToken);
				})
			.ConfigureAwait(false);
	}
}