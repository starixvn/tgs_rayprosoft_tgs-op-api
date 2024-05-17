using System;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator.ApplicationServices.AgentWinLossReport;

public class GetAgentWinLossBreadcrumbQuery
{
    public Guid BrandId { get; set; }
    public Guid AgentId { get; set; }
    public string? Username { get; init; }
    public AccountType MemberType { get; init; }
    public Guid? RoundId { get; init; }
}