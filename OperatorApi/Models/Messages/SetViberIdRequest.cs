using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class SetViberIdRequest
{
	[JsonPropertyName("viber")]
	public string ViberId { get; init; }
}
