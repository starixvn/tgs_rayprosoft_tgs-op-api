using System.Text.Json.Serialization;
using TGS.Core.Models.Messages;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record CreatePlayerResponse : TgsResponse
{
    [JsonPropertyName("uid")]
    public string PlayerCode { get; init; }

    [JsonPropertyName("isnew")]
    public bool IsNew { get; init; }
}
