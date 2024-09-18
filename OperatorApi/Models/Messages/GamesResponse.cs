namespace TGS.Apis.OperatorApi.Models.Messages;

public class GamesResponse
{

	public int PageIndex { get; init; }

	public int PageSize { get; init; }

	public int Total { get; init; }

	public object Key { get; init; } = default!;

	public IEnumerable<GameModel> Games { get; init; } = default!;
}
