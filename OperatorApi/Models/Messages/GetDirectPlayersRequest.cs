#nullable enable

using TGS.Core.Models.Enums;
using TGS.Core.Models.Messages;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class GetDirectPlayersRequest : PageableRequest
{
	public string? PlayerUsername { get; set; }
	public PlayerStatus? PlayerStatus { get; set; }
}