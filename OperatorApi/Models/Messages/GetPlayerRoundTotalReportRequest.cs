using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record GetPlayerRoundTotalReportRequest
{
	[JsonPropertyName("from")]
	public DateTimeOffset From { get; set; }

	[JsonPropertyName("to")]
	public DateTimeOffset To { get; set; }
}
