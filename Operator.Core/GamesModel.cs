using System.Collections.Generic;

namespace TGS.Services.Operator;

public record GamesModel
{
	public int PageIndex { get; init; } = default!;

	public int PageSize { get; init; } = default!;

	public int Total { get; init; } = default!;

	public object Key { get; init; } = default!;

	public IEnumerable<GameInfo> Games { get; init; } = default!;
}
