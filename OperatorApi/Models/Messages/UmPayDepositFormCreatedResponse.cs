using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class UmPayDepositFormCreatedResponse
{
	[JsonPropertyName("errormsg")]
	public string ErrorMessage { get; init; }

	[JsonPropertyName("formid")]
	public Guid FormId { get; init; }

	[JsonPropertyName("paymenturl")]
	public string PaymentUrl { get; init; }
}