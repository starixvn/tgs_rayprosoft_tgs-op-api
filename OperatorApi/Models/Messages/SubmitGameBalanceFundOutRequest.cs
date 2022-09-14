using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record SubmitGameBalanceFundOutRequest
{
    [JsonPropertyName("gpcode")]
    public string GameProviderCode { get; init; }
    [JsonPropertyName("amount")]
    public int Amount { get; init; }
}