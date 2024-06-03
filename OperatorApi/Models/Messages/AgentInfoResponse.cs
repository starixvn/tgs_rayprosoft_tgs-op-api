#nullable enable

namespace TGS.Apis.OperatorApi.Models.Messages;

public class AgentInfoResponse
{
	public decimal DividendPercentage { get; set; }
	public decimal NegativeAgentDividend { get; set; }
	public decimal CardGamePercentage { get; set; }
	public decimal LiveDealerPercentage { get; set; }
	public decimal LotteryPercentage { get; set; }
	public decimal SlotPercentage { get; set; }
	public decimal SportsPercentage { get; set; }
	public decimal VirtualSportsPercentage { get; set; }
	public int DirectAgentCount { get; set; }
	public int IndirectAgentCount { get; set; }
	public int DirectPlayerCount { get; set; }
	public int IndirectPlayerCount { get; set; }
	public string InvitationCode { get; set; } = default!;
	public bool NegativeAgentDividendCarryForward { get; set; }
}