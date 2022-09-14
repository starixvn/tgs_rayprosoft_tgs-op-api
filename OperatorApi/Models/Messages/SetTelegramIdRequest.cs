using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class SetTelegramIdRequest
{
    [JsonPropertyName("telegram")]
    public string TelegramId { get; init; }
}
