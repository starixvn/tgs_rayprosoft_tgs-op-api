using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record SetPayPasswordRequest
{
    [JsonPropertyName("pwd")]
    public string Password { get; init; }
}
