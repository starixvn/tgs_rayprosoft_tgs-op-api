using System.Collections.Generic;
using System.Text.Json;

namespace TGS.Services.Operator;

public record GameInfo
{
	public int Id { get; init; }
	public string Code { get; init; } = default!;
	public string Name { get; init; } = default!;
	public string Description { get; init; } = default!;
	public string GameProviderCode { get; init; } = default!;
	public string SubGameProviderCode { get; init; } = default!;
	public string GameProviderName { get; init; } = default!;
	public bool IsActive { get; init; }
	public Core.Models.Enums.GameType Type { get; init; }
	public TagInfo Tag { get; init; } = default!;
	public int DisplayOrder { get; init; }

	public IDictionary<string, JsonDocument>? Localization { get; init; }
}