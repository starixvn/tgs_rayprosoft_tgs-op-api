#nullable enable

namespace TGS.Apis.OperatorApi.Models;

public class RouletteSetting
{
	public IEnumerable<RouletteItem> RouletteItems { get; init; } = Array.Empty<RouletteItem>();
}
