#nullable enable

using TGS.Core.Models.Enums;
using TGS.Core.Models.Messages;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class GetDirectAgentsRequest : PageableRequest
{
	public string? AgentUsername { get; set; }
	public AgentStatus? AgentStatus { get; set; }
}