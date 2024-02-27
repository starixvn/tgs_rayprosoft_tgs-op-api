using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class StartInitEmailRequest
{
	[Required]
	[JsonPropertyName("email")]
	public string Email { get; init; }
}