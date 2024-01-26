using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class DepositAccumulateRouletteGameRecordViewModel
{
	[JsonPropertyName("createdon")]
	public DateTimeOffset CreatedOn { get; init; }

	[JsonPropertyName("date")]
	public DateOnly Date { get; init; }

	[JsonPropertyName("id")]
	public Guid Id { get; init; }

	[JsonPropertyName("itemindex")]
	public int ItemIndex { get; init; }
}
