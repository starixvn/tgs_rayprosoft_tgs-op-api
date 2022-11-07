using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class InitPhoneRequest
{
	[Required]
	[JsonPropertyName("areacode")]
	public string AreaCode { get; init; }

	[Required]
	[JsonPropertyName("pwd")]
	public string Password { get; init; }

	[Required]
	[JsonPropertyName("phone")]
	public string Phone { get; init; }

	[Required]
	[JsonPropertyName("username")]
	public string UserName { get; init; }
}
