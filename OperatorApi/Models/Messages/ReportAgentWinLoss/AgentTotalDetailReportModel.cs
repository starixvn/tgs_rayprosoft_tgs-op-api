namespace TGS.Apis.OperatorApi.Models.Messages.ReportAgentWinLoss;

public record AgentTotalDetailReportModel
{
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
    public decimal Percentage { get; set; }
    public decimal PlayerWinLoss { get; set; }
    public decimal PlayerSpecial { get; set; }
    public decimal AgentWinLoss { get; set; }
    public decimal AgentSpecial { get; set; }
    public decimal AgentTotal { get; set; }
}