namespace TGS.Apis.OperatorApi.Models.Messages.ReportAgentWinLoss;

public class ExportAgentPlayerWinLossRequest
{
    public string PlayerCode { get; init; }
    public DateTimeOffset ClosedFrom { get; init; }
    public DateTimeOffset ClosedTo { get; init; }
}