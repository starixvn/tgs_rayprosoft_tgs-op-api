using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class CategoryResponse
{

	public int PageIndex { get; init; } = default!;

	public int PageSize { get; init; } = default!;

	public int Total { get; init; } = default!;

	public string CategoryName { get; init; } = default;

	public GameType GameType { get; init; }

	public IEnumerable<GameModel> Games { get; init; } = default!;
}
