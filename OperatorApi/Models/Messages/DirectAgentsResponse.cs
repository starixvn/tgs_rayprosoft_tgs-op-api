#nullable enable

using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class DirectAgentsResponse
{
	public string AgentUsername { get; set; } = string.Empty;
	public string? RealName { get; set; }
	public DateTime? RegisteredOn { get; set; }
	public decimal AgentDividendPercentage { get; set; }
	public int DirectPlayerWithAgentCount { get; set; }
	public AgentStatus AgentStatus { get; set; }
}