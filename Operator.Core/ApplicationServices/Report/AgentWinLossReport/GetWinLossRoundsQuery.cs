using System;
using TGS.Core.Models.Messages;

namespace TGS.Services.Operator.ApplicationServices.Report.AgentWinLossReport;

public class GetWinLossRoundsQuery : PageableRequest
{
	public Guid BrandId { get; init; }
	public Guid AgentId { get; init; }
	public string PlayerCode { get; init; } = default!;
	public DateTimeOffset ClosedFrom { get; init; }
	public DateTimeOffset ClosedTo { get; init; }
}