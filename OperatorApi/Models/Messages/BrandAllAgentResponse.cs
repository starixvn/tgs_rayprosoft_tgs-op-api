using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record BrandAllAgentResponse
{
	[JsonPropertyName("switchon")]
	public bool SwitchOn { get; set; }
}
