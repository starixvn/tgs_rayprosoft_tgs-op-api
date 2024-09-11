using System.Collections.Generic;
using System.Text.Json;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator;

public record GameProviderInfo
{
	public string Code { get; init; } = default!;

	public string Description { get; init; } = default!;

	public Core.Models.Enums.GameType[] GameTypes { get; init; } = default!;

	public bool IsTransferWallet { get; init; }

	public IDictionary<string, JsonDocument>? Localization { get; init; }

	public string Name { get; init; } = default!;

	public Status Status { get; init; }
}