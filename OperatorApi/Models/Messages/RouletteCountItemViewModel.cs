using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class RouletteCountItemViewModel
{
	[JsonPropertyName("promotionid")]
	public Guid PromotionId { get; init; }

	[JsonPropertyName("promotiontype")]
	public string PromotionType { get; init; }

	[JsonPropertyName("roulettecount")]
	public int RouletteCount { get; init; }
}