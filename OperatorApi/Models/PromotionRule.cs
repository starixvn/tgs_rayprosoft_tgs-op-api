#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models;

public class PromotionRule<T>
{
	[JsonPropertyName("compaignDetail")]
	public T? CampaignDetail { get; init; }

	[JsonPropertyName("vipLevels")]
	public IEnumerable<int> VipLevels { get; init; } = Array.Empty<int>();
}
