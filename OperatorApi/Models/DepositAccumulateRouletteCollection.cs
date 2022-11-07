#nullable enable

using System.Collections.Generic;

namespace TGS.Apis.OperatorApi.Models;

public class DepositAccumulateRouletteCollection
{
	public IEnumerable<DepositAccumulateRouletteItem> RouletteItems { get; init; } = default!;
}
