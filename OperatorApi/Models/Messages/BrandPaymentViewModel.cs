using System.Text.Json;
using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class BrandPaymentViewModel
{
	[JsonPropertyName("banktype")]
	public string BankType { get; init; } = default!;

	[JsonPropertyName("channelcode")]
	public string ChannelCode { get; init; } = default!;

	[JsonPropertyName("channelid")]
	public Guid ChannelId { get; init; }

	[JsonPropertyName("channelname")]
	public string ChannelName { get; init; } = default!;

	[JsonPropertyName("displayorder")]
	public int DisplayOrder { get; init; }

	[JsonPropertyName("extended")]
	public JsonDocument ExtendedData { get; init; } = default!;

	[JsonPropertyName("feepercentile")]
	public decimal HandlingFeeRatePercentile { get; init; }

	[JsonPropertyName("id")]
	public Guid Id { get; init; }

	[JsonPropertyName("methodcode")]
	public string MethodCode { get; init; } = default!;

	[JsonPropertyName("methodid")]
	public Guid MethodId { get; init; }

	[JsonPropertyName("paymentcode")]
	public string PaymentCode { get; init; } = default!;

	[JsonPropertyName("paymentid")]
	public Guid PaymentId { get; init; }

	[JsonPropertyName("transferlimit")]
	public ValueRange<decimal> TransferLimit { get; init; }
}
