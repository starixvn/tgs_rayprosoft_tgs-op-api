using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record StartSetEmailRequest
{
	[JsonPropertyName("email")]
	public string Email { get; init; }
	[JsonPropertyName("brand")]
	public string Brand { get; init; }
	[JsonPropertyName("username")]
	public string Username { get; init; }
}