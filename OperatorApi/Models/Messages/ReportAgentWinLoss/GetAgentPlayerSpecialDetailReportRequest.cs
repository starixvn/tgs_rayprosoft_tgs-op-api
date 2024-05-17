namespace TGS.Apis.OperatorApi.Models.Messages.ReportAgentWinLoss;

public record GetAgentPlayerSpecialDetailReportRequest
{
    public Guid AgentId { get; init; }
    public DateTimeOffset From { get; init; }
    public DateTimeOffset To { get; init; }
}