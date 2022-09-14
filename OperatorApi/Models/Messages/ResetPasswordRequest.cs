using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record ResetPasswordRequest
{
    [JsonPropertyName("securitykey")]
    public string SecurityKey { get; init; }

    [JsonPropertyName("validatecode")]
    public string ValidateCode { get; init; }

    [JsonPropertyName("newpwd")]
    public string NewPassword { get; init; }
}
