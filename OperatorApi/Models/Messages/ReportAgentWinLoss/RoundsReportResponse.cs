using TGS.Core.Models.Messages;

namespace TGS.Apis.OperatorApi.Models.Messages.ReportAgentWinLoss;

public class RoundsReportResponse : MessageResponse
{
    public RoundReportModel[] Items { get; init; }

    public RoundTotalReportModel Total { get; init; }

    public int TotalCount { get; init; }
}