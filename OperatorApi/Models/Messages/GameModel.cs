using System.Text.Json.Serialization;
using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record GameModel
{
	[JsonPropertyName("id")]
	public int Id { get; init; }

	[JsonPropertyName("code")]
	public string Code { get; init; }

	[JsonPropertyName("name")]
	public string Name { get; init; }

	[JsonPropertyName("description")]
	public string Description { get; init; }

	[JsonPropertyName("gpcode")]
	public string GameProviderCode { get; init; }

	[JsonPropertyName("gpname")]
	public string GameProviderName { get; init; }

	[JsonPropertyName("sgpcode")]
	public string SubGameProviderCode { get; init; }

	[JsonPropertyName("isactive")]
	public bool IsActive { get; init; }

	[JsonPropertyName("type")]
	public GameType Type { get; init; }

	[JsonPropertyName("gamestatus")]
	public GameStatus GameStatus { get; init; }

	[JsonPropertyName("tag")]
	public Tag Tag { get; init; }

	[JsonIgnore]
	public int DisplayOrder { get; init; }
}