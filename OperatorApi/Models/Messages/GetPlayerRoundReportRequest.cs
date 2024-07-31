using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record GetPlayerRoundReportRequest : PagingRequest
{
	[JsonPropertyName("from")]
	public DateTimeOffset From { get; set; }

	[JsonPropertyName("to")]
	public DateTimeOffset To { get; set; }

	[FromQuery(Name = "culture")]
	[JsonPropertyName("culture")]
	public string CultureName { get; set; }
}