using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class SetZaloRequest
{
	[JsonPropertyName("zalo")]
	public string Zalo { get; init; }
}