using System;
using Tgs.Report.Client.Models;

namespace TGS.Services.Operator.ApplicationServices.Report.AgentContributionReport;

public record GetAgentContributionQuery : PageableRequest
{
	public Guid BrandId { get; init; }
	public Guid AgentId { get; init; }
	public bool IncludeAgents { get; init; }
	public DateTimeOffset From { get; init; }
	public DateTimeOffset To { get; init; }
}