#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class DepositAccumulateRouletteSettingViewModel
{
	[JsonPropertyName("content")]
	public string? Content { get; init; }

	[JsonPropertyName("from")]
	public DateTime From { get; init; }

	[JsonPropertyName("localization")]
	public JsonDocument Localization { get; init; } = default!;

	[JsonPropertyName("name")]
	public string Name { get; init; } = default!;

	[JsonPropertyName("rouletteitems")]
	public IEnumerable<DepositAccumulateRouletteItemViewModel> RouletteItems { get; init; } =
		Array.Empty<DepositAccumulateRouletteItemViewModel>();

	[JsonPropertyName("subsubject")]
	public string? SubSubject { get; init; }

	[JsonPropertyName("terms")]
	public string? Terms { get; init; }

	[JsonPropertyName("to")]
	public DateTime To { get; init; }

	[JsonPropertyName("viplevels")]
	public IEnumerable<int> VipLevels { get; init; } = Array.Empty<int>();
}
