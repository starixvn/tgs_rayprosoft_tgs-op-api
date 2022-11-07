using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class SubmitInitPhoneRequest
{
	[JsonPropertyName("securitykey")]
	public string SecurityKey { get; init; }

	[JsonPropertyName("validatecode")]
	public string ValidateCode { get; init; }
}
