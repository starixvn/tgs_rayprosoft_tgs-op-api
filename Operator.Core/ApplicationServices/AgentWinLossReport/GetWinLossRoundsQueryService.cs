using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Agent.Client;
using Tgs.Agent.Client.Models;
using TGS.Core.Models.Enums;
using Tgs.PlayerService.Client;
using Tgs.Report.Client;
using Tgs.Report.Client.Models.Round;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.AgentWinLossReport;

public class
    GetWinLossRoundsQueryService(
        IReportClient reportClient,
        IPlayerServiceClient playerClient,
        IAgentClient agentClient)
    : IGetService<GetWinLossRoundsQuery,
        RoundsResponse>
{
    public async ValueTask<RoundsResponse> GetAsync(
        GetWinLossRoundsQuery request,
        CancellationToken cancellationToken = default)
    {
        var playerProperties =
            await playerClient.GetPlayerPropertiesAsync(request.PlayerCode,
                cancellationToken).ConfigureAwait(false);

        _ = Guid.TryParse(playerProperties["AgentId"], out var agentId);

        var childAgents =
            agentClient.GetChildAgentInfosAsync(new GetChildAgentInfosRequest(request.AgentId), cancellationToken);

        var inAgentLine =
            request.AgentId == agentId ||
            await childAgents.AnyAsync(x => x.AgentId == agentId, cancellationToken)
                .ConfigureAwait(false);

        if (!inAgentLine) return new RoundsResponse();

        var response = await reportClient.GetRoundsAsync(new GetRoundsRequest
        {
            BrandId = request.BrandId,
            PlayerCode = request.PlayerCode,
            ClosedFrom = request.ClosedFrom,
            ClosedTo = request.ClosedTo,
            Page = request.Page,
            Rows = request.Rows,
            OrderBy = (short)request.OrderBy,
            SortBy = request.SortBy,
            ShowAll = request.ShowAll
        }).ConfigureAwait(false);

        var responseTotal = await reportClient.GetRoundsTotalAsync(new GetRoundsTotalRequest
        {
            BrandId = request.BrandId,
            PlayerCode = request.PlayerCode,
            ClosedFrom = request.ClosedFrom,
            ClosedTo = request.ClosedTo
        }).ConfigureAwait(false);

        return new RoundsResponse
        {
            Valid = response.Valid,
            TotalCount = response.TotalCount,
            Items = response.Items.Select(x => new RoundModel
            {
                Id = x.Id,
                Status = (ClientRoundStatus)x.Status,
                PlayerCode = x.PlayerCode,
                ExternalPlayerCode = x.ExternalPlayerCode,
                PlayerName = x.PlayerName,
                GameProviderId = x.GameProviderId,
                GameProviderCode = x.GameProviderCode,
                GameProviderName = x.GameProviderName,
                SubGameProviderName = x.SubGameProviderName,
                GameId = x.GameId,
                GameName = x.GameName,
                ExternalRoundId = x.ExternalRoundId,
                ExternalCreatedOn = x.ExternalCreatedOn,
                ExternalClosedOn = x.ExternalClosedOn,
                CreatedOn = x.CreatedOn,
                ClosedOn = x.ClosedOn,
                CurrencyCode = x.CurrencyCode,
                BetAmount = x.BetAmount,
                ValidBetAmount = x.ValidBetAmount,
                Turnover = x.Turnover,
                SettleAmount = x.SettleAmount,
                WinLoss = x.WinLoss,
                AdditionalData = x.AdditionalData
            }).ToArray(),
            Total = new RoundTotalModel
            {
                BetAmount = responseTotal.BetAmount ?? 0M,
                ValidBetAmount = responseTotal.ValidBetAmount ?? 0M,
                WinLoss = responseTotal.WinLoss ?? 0M,
                SettleAmount = responseTotal.SettleAmount ?? 0M
            }
        };
    }
}