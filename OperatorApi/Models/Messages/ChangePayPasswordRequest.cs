using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record ChangePayPasswordRequest
{
	[JsonPropertyName("newpwd")]
	public string NewPassword { get; init; }

	[JsonPropertyName("validatecode")]
	public string ValidateCode { get; init; }

	[JsonPropertyName("securitykey")]
	public string SecurityKey { get; init; }
}
