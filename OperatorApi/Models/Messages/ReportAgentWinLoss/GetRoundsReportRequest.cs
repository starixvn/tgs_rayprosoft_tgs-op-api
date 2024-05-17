using TGS.Core.Models.Messages;

namespace TGS.Apis.OperatorApi.Models.Messages.ReportAgentWinLoss;

public class GetRoundsReportRequest : PageableRequest
{
    public string PlayerCode { get; init; }

    public DateTimeOffset ClosedFrom { get; init; }

    public DateTimeOffset ClosedTo { get; init; }
}