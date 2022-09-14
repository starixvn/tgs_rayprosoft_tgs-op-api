using System.Text.Json.Serialization;
using TGS.Core.Models.Messages;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record GetGameBalanceResponse : TgsResponse
{
    [JsonPropertyName("balance")]
    public decimal Balance { get; init; }
}