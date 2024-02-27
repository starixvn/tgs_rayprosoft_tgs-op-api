using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class SetLineIdRequest
{
	[JsonPropertyName("line")]
	public string LineId { get; init; }
}