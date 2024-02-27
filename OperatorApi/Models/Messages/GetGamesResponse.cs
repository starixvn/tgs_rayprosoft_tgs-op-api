using System.Text.Json.Serialization;
using TGS.Core.Models.Messages;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record GetGamesResponse : TgsResponse
{
	[JsonPropertyName("data")]
	public GameModel[] Games { get; init; }
}