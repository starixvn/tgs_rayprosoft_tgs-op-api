using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record AuthRequest
{
    [Required]
    [JsonPropertyName("client")]
    public string ApiKey { get; init; }

    [Required]
    [JsonPropertyName("currency")]
    public string Currency { get; init; }
}