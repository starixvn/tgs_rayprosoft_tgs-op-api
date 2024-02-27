using System.Text.Json.Serialization;
using TGS.Core.Models.Messages;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record JoinPromotionResponse : TgsResponse
{
	[JsonPropertyName("success")]
	public bool Success { get; init; }
}