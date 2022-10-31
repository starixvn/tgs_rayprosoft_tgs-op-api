using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class StartInitPhoneRequest
{
	[Required]
	[JsonPropertyName("areacode")]
	public string AreaCode { get; init; }

	[Required]
	[JsonPropertyName("phone")]
	public string Phone { get; init; }
}
