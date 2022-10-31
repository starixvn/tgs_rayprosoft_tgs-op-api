using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record ManualDepositFpsFormResponse : ManualDepositFormResponse
{
	[JsonPropertyName("qrcodefileurl")]
	public string QrCodeFileUrl { get; init; }

	[JsonPropertyName("email")]
	public string Email { get; init; }

	[JsonPropertyName("fpsid")]
	public string FpsId { get; init; }

	[JsonPropertyName("phone")]
	public string Phone { get; init; }
}
