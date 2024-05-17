namespace TGS.Apis.OperatorApi.Models.Messages.ReportAgentWinLoss;

public record AgentWinLossBreadcrumbReportModel
{
    public string Username { get; set; } = default!;
    public string Id { get; set; } = default!;
    public string MemberType { get; set; } = default!;
}