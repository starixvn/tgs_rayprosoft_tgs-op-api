namespace TGS.Apis.OperatorApi.Models.Messages.ReportPlayerAllAgent;

public class PlayerAllAgentRebateReportModel
{
    public int MaxActivePlayerNumber { get; set; }
    public decimal ValidBet { get; set; }
    public decimal Rebate { get; set; }
    public DateTimeOffset SettleDateTime { get; set; }
    public DateTimeOffset? SentOn { get; set; }
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
}
