using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record LeePayDepositFormCreatedResponse
{
	[JsonPropertyName("formid")]
	public Guid FormId { get; init; }

	[JsonPropertyName("paymenturl")]
	public string PaymentUrl { get; init; }
}