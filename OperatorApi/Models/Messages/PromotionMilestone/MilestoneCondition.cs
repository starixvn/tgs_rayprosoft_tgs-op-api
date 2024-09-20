namespace TGS.Apis.OperatorApi.Models.Messages.PromotionMilestone;

public class MilestoneCondition
{
	public decimal DepositAmount { get; init; }
	public decimal ValidBetAmount { get; init; }
	public decimal Bonus { get; init; }
	public bool IsSent { get; init; }
}