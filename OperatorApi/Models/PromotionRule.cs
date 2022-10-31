#nullable enable

using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models;

public class PromotionRule<T>
{
	[JsonPropertyName("compaignDetail")]
	public T? CampaignDetail { get; init; }
}
