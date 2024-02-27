#nullable enable

using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models;

public class DepositAccumulateRouletteSettings
{
	[JsonPropertyName("compaignDetail")]
	public DepositAccumulateRouletteCollection CampaignDetail { get; init; } = default!;

	[JsonPropertyName("vipLevels")]
	public IEnumerable<int> VipLevels { get; init; } = default!;
}