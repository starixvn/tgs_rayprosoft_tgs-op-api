using TGS.Core.Models.Enums;
using TGS.Core.Models.Messages;

namespace TGS.Apis.OperatorApi.Models.Messages.ReportAgentWinLoss;

public class GetAgentWinLossReportRequest : PageableRequest
{
    public string Username { get; init; }
    public AccountType MemberType { get; init; }
    public bool DownLine { get; init; }
    public DateTimeOffset From { get; init; }
    public DateTimeOffset To { get; init; }
}