using System;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Messages;

namespace TGS.Services.Operator.ApplicationServices.Report.AgentWinLossReport;

public class GetAgentWinLossQuery : PageableRequest
{
	public Guid BrandId { get; init; }
	public Guid AgentId { get; init; }
	public string Username { get; init; } = default!;
	public AccountType MemberType { get; init; }
	public bool DownLine { get; init; }
	public DateTimeOffset From { get; init; }
	public DateTimeOffset To { get; init; }
}