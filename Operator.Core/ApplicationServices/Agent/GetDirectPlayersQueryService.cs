using System.Collections.Generic;
using System.Threading;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Agent;

internal class GetDirectPlayersQueryService(
	IPlayerServiceClient playerServiceClient)
	: IQueryService<GetDirectPlayersQuery, PlayerListItem>
{
	public IAsyncEnumerable<PlayerListItem> QueryAsync(
		GetDirectPlayersQuery query,
		CancellationToken cancellationToken = default) =>
		playerServiceClient.QueryPlayerListAsync(
			query.BrandId,
			new PlayerListQuery
			{
				AgentId = query.AgentId, Status = (PlayerStatus?)query.PlayerStatus, UserName = query.PlayerUsername
			},
			cancellationToken);
}