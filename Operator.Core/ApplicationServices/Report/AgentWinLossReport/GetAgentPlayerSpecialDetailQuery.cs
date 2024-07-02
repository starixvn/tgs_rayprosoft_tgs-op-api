using System;

namespace TGS.Services.Operator.ApplicationServices.Report.AgentWinLossReport;

public record GetAgentPlayerSpecialDetailQuery
{
	public Guid BrandId { get; init; }
	public Guid AgentId { get; init; }
	public DateTimeOffset From { get; init; }
	public DateTimeOffset To { get; init; }
}