namespace TGS.Apis.OperatorApi.Models.Messages.ReportAgentWinLoss;

public record CompanyTotalDetailReportModel
{
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
    public decimal Percentage { get; set; }
    public decimal PlayerWinLoss { get; set; }
    public decimal PlayerSpecial { get; set; }
    public decimal CompanyWinLoss { get; set; }
    public decimal CompanySpecial { get; set; }
    public decimal CompanyTotal { get; set; }
}