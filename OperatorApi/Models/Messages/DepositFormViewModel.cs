using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record DepositFormViewModel
{
	[JsonPropertyName("id")]
	public Guid Id { get; init; }

	[JsonPropertyName("status")]
	public FlowState Status { get; init; }

	[JsonPropertyName("amount")]
	public decimal Amount { get; init; }

	[JsonPropertyName("brandpaymentid")]
	public Guid BrandPaymentId { get; init; }

	[JsonPropertyName("createdon")]
	public DateTimeOffset CreatedOn { get; init; }

	[JsonPropertyName("updatedon")]
	public DateTimeOffset UpdatedOn { get; init; }

	[JsonPropertyName("canuploadcerts")]
	public bool CanUploadCerts { get; init; }

	[JsonPropertyName("orderno")]
	public string OrderNo { get; init; }
}
