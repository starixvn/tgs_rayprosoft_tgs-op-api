using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record GetPlayerRoundReportRequest : PagingRequest
{
	[JsonPropertyName("from")]
	public DateTimeOffset From { get; set; }

	[JsonPropertyName("to")]
	public DateTimeOffset To { get; set; }

	[JsonPropertyName("culture")]
	public string CultureName { get; set; }
}