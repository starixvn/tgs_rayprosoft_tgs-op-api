using System.Text.Json.Serialization;
using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record GameProviderModel
{
	[JsonPropertyName("code")]
	public string Code { get; init; }

	[JsonPropertyName("description")]
	public string Description { get; init; }

	[JsonPropertyName("gametypes")]
	public GameType[] GameTypes { get; init; }

	[JsonPropertyName("transferwallet")]
	public bool IsTransferWallet { get; init; }

	[JsonPropertyName("name")]
	public string Name { get; init; }

	[JsonPropertyName("status")]
	public Status Status { get; init; }
}
