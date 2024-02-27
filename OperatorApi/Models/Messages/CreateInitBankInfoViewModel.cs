using System.Text.Json;
using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class CreateInitBankInfoViewModel
{
	[JsonPropertyName("banktype")]
	public string BankType { get; init; }

	[JsonPropertyName("detail")]
	public JsonDocument Detail { get; init; }

	[JsonPropertyName("paymentchannelid")]
	public Guid PaymentChannelId { get; init; }
}