using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record BalanceInfoResponse
{
	[JsonPropertyName("main")]
	public decimal Main { get; init; }
}