#nullable enable

namespace TGS.Apis.OperatorApi.Models;

public class DepositAccumulateRouletteCollection
{
	public IEnumerable<DepositAccumulateRouletteItem> RouletteItems { get; init; } = default!;
}
