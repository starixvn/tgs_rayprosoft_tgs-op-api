namespace TGS.Apis.OperatorApi.Models.Messages.ReportAgentWinLoss;

public record AgentPlayerSpecialDetailReportModel
{
    public decimal Bonus { get; set; }
    public decimal BonusAdjust { get; set; }
    public decimal CompanyAdjust { get; set; }
    public decimal AgentAdjust { get; set; }
}