#nullable enable

using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages.ReportAgentWinLoss;

public class GetAgentWinLossBreadcrumbReportRequest
{
    public string? Username { get; init; }
    public AccountType MemberType { get; init; }
    public Guid? RoundId { get; init; }
}