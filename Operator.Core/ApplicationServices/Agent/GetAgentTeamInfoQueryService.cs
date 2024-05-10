using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Agent.Client;
using Tgs.Agent.Client.Models;
using Tgs.PlayerService.Client;
using TGS.Services.Agent;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Agent;

internal class GetAgentTeamInfoQueryService(
	IAgentClient agentClient,
	IPlayerServiceClient playerServiceClient)
	: IGetService<GetAgentTeamInfoQuery, GetAgentTeamInfoModel?>
{
    private readonly IAgentClient _agentClient = agentClient ?? throw new ArgumentNullException(nameof(agentClient));
    private readonly IPlayerServiceClient _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));

    public async ValueTask<GetAgentTeamInfoModel?> GetAsync(GetAgentTeamInfoQuery query,
        CancellationToken cancellationToken = default)
    {
        var allChildAgents = await _agentClient.GetChildAgentInfosAsync(new GetChildAgentInfosRequest(query.AgentId), cancellationToken)
            .ToArrayAsync(cancellationToken);

        var directAgents = allChildAgents.Where(x => x.ParentAgentId == query.AgentId);

        var indirectAgents = allChildAgents.Where(x => x.ParentAgentId != query.AgentId);

        var agentPlayerCounts =
            await _playerServiceClient.GetPlayerCountInfosByBrandIdAsync(query.BrandId, false, cancellationToken)
                .ToArrayAsync(cancellationToken);

        var directPlayerCountInfo =
            agentPlayerCounts.FirstOrDefault(x => x.AgentId == query.AgentId);

        var allChildAgentIds = allChildAgents.Select(x => x.AgentId);

        var indirectPlayerCount =
            agentPlayerCounts.Where(x => x.AgentId != null && allChildAgentIds.Contains(x.AgentId.Value))
                .Sum(x => x.PlayerCount);

        return new()
        {
            DirectAgentCount = directAgents.Count(),
            DirectPlayerCount = directPlayerCountInfo?.PlayerCount ?? 0,
            IndirectAgentCount = indirectAgents.Count(),
            IndirectPlayerCount = indirectPlayerCount
        };
    }
}
