using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class TransactionLogViewModel
{
	[JsonPropertyName("amount")]
	public decimal Amount { get; set; }

	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("recordedon")]
	public DateTime RecordedOn { get; set; }

	[JsonPropertyName("type")]
	public string TransactionType { get; set; }
}
