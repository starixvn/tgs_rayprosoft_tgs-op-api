using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class SetFacebookMessengerIdRequest
{
    [JsonPropertyName("facebookmessenger")]
    public string FacebookMessenger { get; init; }
}
