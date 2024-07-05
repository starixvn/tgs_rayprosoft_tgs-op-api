using TGS.Core.Models.Enums;
using Tgs.Report.Client.Models;

namespace TGS.Apis.OperatorApi.Models.Messages.ReportAgentWinLoss;

public record GetAgentWinLossReportRequest : PageableRequest
{
    public string Username { get; init; }
    public AccountType MemberType { get; init; }
    public bool DownLine { get; init; }
    public DateTimeOffset From { get; init; }
    public DateTimeOffset To { get; init; }
}