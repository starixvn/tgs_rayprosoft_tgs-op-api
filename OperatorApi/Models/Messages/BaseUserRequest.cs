using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public abstract record BaseUserRequest
{
	[Required]
	[JsonPropertyName("username")]
	public string Username { get; init; }
}
