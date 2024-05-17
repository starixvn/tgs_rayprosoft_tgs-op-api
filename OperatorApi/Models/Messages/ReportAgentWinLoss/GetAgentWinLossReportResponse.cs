using TGS.Core.Models.Messages;

namespace TGS.Apis.OperatorApi.Models.Messages.ReportAgentWinLoss;

public class GetAgentWinLossReportResponse : MessageResponse
{
    public AgentWinLossReportModel[] Items { get; init; }

    public AgentWinLossReportTotalModel Total { get; init; }

    public int TotalCount { get; init; }
}