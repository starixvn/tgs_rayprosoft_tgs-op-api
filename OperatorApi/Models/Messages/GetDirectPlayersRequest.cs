#nullable enable

using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record GetDirectPlayersRequest : PagingRequest
{
	public string? PlayerUsername { get; set; }
	public PlayerStatus? PlayerStatus { get; set; }
}