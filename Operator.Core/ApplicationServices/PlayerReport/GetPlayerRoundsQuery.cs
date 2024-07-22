using System;
using Tgs.Report.Client.Models;

namespace TGS.Services.Operator.ApplicationServices.PlayerReport;

public record GetPlayerRoundsQuery : PageableRequest
{
	public Guid BrandId { get; init; }
	public string? PlayerCode { get; init; }
	public DateTimeOffset From { get; init; }
	public DateTimeOffset To { get; init; }
	public required string CultureName { get; init; }
}


