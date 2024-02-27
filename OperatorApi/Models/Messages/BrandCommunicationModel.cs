using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record BrandCommunicationModel
{
	public string Type { get; init; }
	public string Phone { get; init; }

	[JsonPropertyName("appid")]
	public string AppId { get; init; }

	[JsonPropertyName("qrcodeurl")]
	public string QRCodeUrl { get; init; }
}