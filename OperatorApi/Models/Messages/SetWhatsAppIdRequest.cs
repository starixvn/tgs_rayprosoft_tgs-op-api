using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class SetWhatsAppIdRequest
{
	[JsonPropertyName("whatsapp")]
	public string WhatsAppId { get; init; }
}
