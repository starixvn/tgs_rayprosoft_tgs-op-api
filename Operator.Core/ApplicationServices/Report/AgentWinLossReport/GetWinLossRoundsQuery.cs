using System;
using Tgs.Report.Client.Models;

namespace TGS.Services.Operator.ApplicationServices.Report.AgentWinLossReport;

public record GetWinLossRoundsQuery : PageableRequest
{
	public Guid BrandId { get; init; }
	public Guid AgentId { get; init; }
	public string PlayerCode { get; init; } = default!;
	public DateTimeOffset ClosedFrom { get; init; }
	public DateTimeOffset ClosedTo { get; init; }
}