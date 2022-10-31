using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class DepositAccumulateRouletteResultViewModel
{
	[JsonPropertyName("itemindex")]
	public int ItemIndex { get; init; }
}
