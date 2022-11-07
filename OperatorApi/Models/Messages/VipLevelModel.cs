namespace TGS.Apis.OperatorApi.Models.Messages;

public class VipLevelModel
{
	public decimal BirthBenefit { get; init; }

	/// <summary>
	/// 棋牌返水比例(%)
	/// </summary>
	public decimal CardGameRebate { get; init; }

	public int Level { get; init; }

	/// <summary>
	/// 等級維持條件
	/// 累積投注金額(每季)
	/// </summary>
	public decimal LevelKeepAccumulateBetAmount { get; init; }

	/// <summary>
	/// 等級維持條件
	/// 累積充值金額(每季)
	/// </summary>
	public decimal LevelKeepAccumulateDeposit { get; init; }

	/// <summary>
	/// 升級條件
	/// 累積投注金額(當月)
	/// </summary>
	public decimal LevelUpAccumulateBetAmount { get; init; }

	/// <summary>
	/// 升級條件
	/// 累積充值金額(當月)
	/// </summary>
	public decimal LevelUpAccumulateDeposit { get; init; }

	/// <summary>
	/// 真人返水比例(%)
	/// </summary>
	public decimal LiveDealerRebate { get; init; }

	/// <summary>
	/// 彩票返水比例(%)
	/// </summary>
	public decimal LotteryRebate { get; init; }

	/// <summary>
	/// 晉升福利
	/// </summary>
	public decimal PromotionBenefit { get; init; }

	/// <summary>
	/// 季度福利
	/// </summary>
	public decimal QuarterlyBenefit { get; init; }

	/// <summary>
	/// 電子返水比例(%)
	/// </summary>
	public decimal SlotRebate { get; init; }

	/// <summary>
	/// 體育返水比例(%)
	/// </summary>
	public decimal SportsRebate { get; init; }

	/// <summary>
	/// 虛擬體育返水比例(%)
	/// </summary>
	public decimal VirtualSportsRebate { get; init; }
}
