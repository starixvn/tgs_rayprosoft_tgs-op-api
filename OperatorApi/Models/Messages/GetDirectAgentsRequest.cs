#nullable enable

using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record GetDirectAgentsRequest : PagingRequest
{
	public string? AgentUsername { get; set; }
	public AgentStatus? AgentStatus { get; set; }
}