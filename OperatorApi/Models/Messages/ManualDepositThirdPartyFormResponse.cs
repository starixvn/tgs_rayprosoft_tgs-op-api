using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record ManualDepositThirdPartyFormResponse : ManualDepositFormResponse
{
    [JsonPropertyName("qrcode")]
    public string QrCode { get; init; }
}
