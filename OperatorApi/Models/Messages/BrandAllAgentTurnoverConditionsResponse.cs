using System.Text.Json.Serialization;
using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record BrandAllAgentTurnoverConditionsResponse
{
	[JsonPropertyName("rank")]
	public int Rank { get; set; }

	[JsonPropertyName("allagentgametype")]
	public AllAgentGameType AllAgentGameType { get; set; }

	[JsonPropertyName("turnover")]
	public decimal Turnover { get; set; }

	[JsonPropertyName("activenumber")]
	public int ActiveNumber { get; set; }

	[JsonPropertyName("rebatepercentage")]
	public decimal RebatePercentage { get; set; }
}
