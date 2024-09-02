using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record LoginGoogleRequest
{
	[Required]
	[JsonPropertyName("token")]
	public string Token { get; init; }

	[Required]
	[JsonPropertyName("platformtype")]
	public PlatformType PlatformType { get; init; }
}
