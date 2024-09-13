using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class ProviderGameListModel
{
	[JsonPropertyName("pageindex")]
	public int PageIndex { get; init; }

	[JsonPropertyName("gpcpde")]
	public string ProviderCode { get; init; }

	[JsonPropertyName("totalgame")]
	public int Total { get; init; }

	[JsonPropertyName("games")]
	public IEnumerable<GameModel> Games { get; init; }
}
