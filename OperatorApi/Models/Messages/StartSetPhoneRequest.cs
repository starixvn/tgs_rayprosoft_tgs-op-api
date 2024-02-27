using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record StartSetPhoneRequest
{
	[JsonPropertyName("area")]
	public string AreaCode { get; init; }

	[JsonPropertyName("phone")]
	public string Phone { get; init; }
}