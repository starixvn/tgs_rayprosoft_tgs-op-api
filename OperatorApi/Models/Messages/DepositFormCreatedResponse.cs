using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class DepositFormCreatedResponse
{
	[JsonPropertyName("formid")]
	public Guid FormId { get; init; }

	[JsonPropertyName("paymenturl")]
	public string PaymentUrl { get; init; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	[JsonPropertyName("paymenturl")]
	public double? ExpirationTime { get; set; }
}