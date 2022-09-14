using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record SetQqIdRequest
{
    [JsonPropertyName("qq")]
    public string QqId { get; init; }
}
