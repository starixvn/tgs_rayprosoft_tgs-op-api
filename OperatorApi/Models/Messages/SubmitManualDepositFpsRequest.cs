using System.Text.Json.Serialization;

#nullable enable

namespace TGS.Apis.OperatorApi.Models.Messages;

public record SubmitManualDepositFpsRequest
{
	[JsonPropertyName("account")]
	public string? BankAccount { get; init; }

	[JsonPropertyName("name")]
	public string? BankName { get; init; }

	[JsonPropertyName("amount")]
	public decimal Amount { get; init; }

	[JsonPropertyName("paymentid")]
	public Guid BrandPaymentId { get; init; }

	[JsonPropertyName("promotionid")]
	public Guid? PromotionId { get; init; }

	[JsonPropertyName("banktype")]
	public string BankType { get; init; } = default!;
}