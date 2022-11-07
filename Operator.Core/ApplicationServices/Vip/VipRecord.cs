namespace TGS.Services.Operator.ApplicationServices.Vip;

public class VipRecord
{
	public decimal BirthBenefit { get; init; }

	public decimal CardGameRebate { get; init; }

	public int Level { get; set; }

	public decimal LevelKeepAccumulateBetAmount { get; init; }

	public decimal LevelKeepAccumulateDeposit { get; init; }

	public decimal LevelUpAccumulateBetAmount { get; init; }

	public decimal LevelUpAccumulateDeposit { get; init; }

	public decimal LiveDealerRebate { get; init; }

	public decimal LotteryRebate { get; init; }

	public decimal PromotionBenefit { get; init; }

	public decimal QuarterlyBenefit { get; init; }

	public decimal SlotRebate { get; init; }

	public decimal SportsRebate { get; init; }

	public decimal VirtualSportsRebate { get; init; }
}
