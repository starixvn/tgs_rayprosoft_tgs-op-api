using System.Text.Json;
using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record CreateBankInfoRequest
{
	[JsonPropertyName("paymentchannelid")]
	public Guid PaymentChannelId { get; init; }

	[JsonPropertyName("banktype")]
	public string BankType { get; init; }

	[JsonPropertyName("detail")]
	public JsonDocument Detail { get; init; }
}
