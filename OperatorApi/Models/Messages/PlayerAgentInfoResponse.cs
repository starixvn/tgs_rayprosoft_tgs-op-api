#nullable enable

namespace TGS.Apis.OperatorApi.Models.Messages;

public class PlayerAgentInfoResponse
{
	public decimal CardGamePercentage { get; set; }
	public decimal LiveDealerPercentage { get; set; }
	public decimal LotteryPercentage { get; set; }
	public decimal SlotPercentage { get; set; }
	public decimal SportsPercentage { get; set; }
	public decimal VirtualSportsPercentage { get; set; }
}