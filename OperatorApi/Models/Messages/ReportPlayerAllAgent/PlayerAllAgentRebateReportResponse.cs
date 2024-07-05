namespace TGS.Apis.OperatorApi.Models.Messages.ReportPlayerAllAgent;

public record PlayerAllAgentRebateReportResponse
{
    public IEnumerable<PlayerAllAgentRebateReportModel> Items { get; init; }

    public PlayerAllAgentRebateTotalAmountReportModel Total { get; init; }

    public int TotalCount { get; init; }
}