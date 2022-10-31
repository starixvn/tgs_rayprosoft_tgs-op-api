using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record SubmitGameBalanceFundInRequest
{
	[JsonPropertyName("gpcode")]
	public string GameProviderCode { get; init; }
	[JsonPropertyName("amount")]
	public int Amount { get; init; }
}
