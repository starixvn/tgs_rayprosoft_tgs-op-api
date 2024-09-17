using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class GameTypeResponse
{

	public int PageIndex { get; init; }

	public int PageSize { get; init; }

	public int Total { get; init; }

	public string GameTypeName { get; init; }

	public string ProviderCode { get; init; } = default!;

	public GameType? GameType { get; init; }

	public IEnumerable<GameModel> Games { get; init; } = default!;
}
