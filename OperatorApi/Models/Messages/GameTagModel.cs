using System.Text.Json.Serialization;
using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class GameTagModel
{
	[JsonPropertyName("limit")]
	public int Limit { get; init; }

	[JsonPropertyName("tag")]
	public GameTag Tag { get; init; }

	[JsonPropertyName("games")]
	public IEnumerable<GameModel> Games { get; init; }

}
