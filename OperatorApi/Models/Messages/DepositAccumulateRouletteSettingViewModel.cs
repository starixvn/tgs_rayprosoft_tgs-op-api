#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class DepositAccumulateRouletteSettingViewModel
{
	[JsonPropertyName("content")]
	public string? Content { get; init; }

	[JsonPropertyName("localization")]
	public JsonDocument Localization { get; init; } = default!;

	[JsonPropertyName("name")]
	public string Name { get; init; } = default!;

	[JsonPropertyName("period")]
	public ValueRange<DateTime> Period { get; init; }

	[JsonPropertyName("rouletteitems")]
	public IEnumerable<DepositAccumulateRouletteItemViewModel> RouletteItems { get; init; } = default!;

	[JsonPropertyName("subsubject")]
	public string? SubSubject { get; init; }

	[JsonPropertyName("terms")]
	public string? Terms { get; init; }

	[JsonPropertyName("viplevels")]
	public IEnumerable<int> VipLevels { get; init; } = default!;
}