using System.Text.Json;
using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record StationLetterResponse
{
	[JsonPropertyName("id")]
	public Guid Id { get; init; }

	[JsonPropertyName("createon")]
	public DateTimeOffset CreateOn { get; init; }

	[JsonPropertyName("subject")]
	public string Subject { get; init; }

	[JsonPropertyName("content")]
	public string Content { get; init; }

	[JsonPropertyName("isread")]
	public bool IsRead { get; init; }

	[JsonPropertyName("localization")]
	public JsonDocument Localization { get; init; }
}
