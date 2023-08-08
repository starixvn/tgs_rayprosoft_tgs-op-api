using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record CreateIdentityAuthenticationFormRequest
{
	[Required]
	[JsonPropertyName("realname")]
	public string RealName { get; init; }

	[Required]
	[JsonPropertyName("id")]
	public string Identity { get; init; }
}
