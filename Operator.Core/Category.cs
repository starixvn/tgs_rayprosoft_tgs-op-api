using System.Collections.Generic;

namespace TGS.Services.Operator;
public record Category
{
	public int PageIndex { get; init; } = default!;

	public int PageSize { get; init; } = default!;

	public int Total { get; init; } = default!;

	public string CategoryName { get; init; } = default;

	public IEnumerable<GameInfo> Games { get; init; } = default!;
}
