using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record StartSetEmailRequest
{
	[JsonPropertyName("email")]
	public string Email { get; init; }
}
