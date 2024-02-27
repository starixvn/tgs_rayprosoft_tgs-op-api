using System.Text.Json.Serialization;

#nullable enable

namespace TGS.Apis.OperatorApi.Models.Messages;

public record SubmitManualDepositCoinsphRequest
{
	[JsonPropertyName("amount")]
	public decimal Amount { get; init; }

	[JsonPropertyName("paymentid")]
	public Guid BrandPaymentId { get; init; }

	[JsonPropertyName("promotionid")]
	public Guid? PromotionId { get; init; }

	[JsonPropertyName("banktype")]
	public string BankType { get; init; } = default!;

	[JsonPropertyName("name")]
	public string? Name { get; init; }

	[JsonPropertyName("phone")]
	public string? Phone { get; init; }

	[JsonPropertyName("address")]
	public string? Address { get; init; }
}