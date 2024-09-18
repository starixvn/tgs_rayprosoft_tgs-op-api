using System.Text.Json.Serialization;
using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class GameTagModel
{
	[JsonPropertyName("pageIndex")]
	public int PageIndex { get; init; }

	[JsonPropertyName("pageSize")]
	public int PageSize { get; init; }

	[JsonPropertyName("tag")]
	public GameTag Tag { get; init; }

	[JsonPropertyName("total")]
	public int Total { get; init; }

	[JsonPropertyName("games")]
	public IEnumerable<GameModel> Games { get; init; }

}
