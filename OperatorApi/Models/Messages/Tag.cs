using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record Tag
{
	[JsonPropertyName("new")]
	public bool New { get; init; }

	[JsonPropertyName("hot")]
	public bool Hot { get; init; }

	[JsonPropertyName("recommend")]
	public bool Recommend { get; init; }
}