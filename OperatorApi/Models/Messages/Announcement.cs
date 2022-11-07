using System.Text.Json;
using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record Announcement
{
	[JsonPropertyName("subject")]
	public string Subject { get; init; }

	[JsonPropertyName("content")]
	public string Content { get; init; }

	[JsonPropertyName("from")]
	public long From { get; init; }

	[JsonPropertyName("to")]
	public long To { get; init; }

	[JsonPropertyName("localization")]
	public JsonDocument Localization { get; init; }

	[JsonPropertyName("type")]
	public string Type { get; init; }
}
