using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator;

public record GameInfo
{
	[JsonPropertyName("id")]
	public int Id { get; init; }

	[JsonPropertyName("code")]
	public string Code { get; init; } = default!;

	[JsonPropertyName("name")]
	public string Name { get; init; } = default!;

	[JsonPropertyName("description")]
	public string Description { get; init; } = default!;

	[JsonPropertyName("gpcode")]
	public string GameProviderCode { get; init; } = default!;

	[JsonPropertyName("isactive")]
	public bool IsActive { get; init; }

	[JsonPropertyName("type")]
	public GameType Type { get; init; }

	[JsonPropertyName("tag")]
	public TagInfo Tag { get; init; } = default!;

	[JsonPropertyName("displayorder")]
	public int DisplayOrder { get; init; }

	[JsonPropertyName("localization")]
	public IDictionary<string, JsonDocument>? Localization { get; init; }
}