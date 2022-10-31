namespace TGS.Apis.OperatorApi.Models.Messages;

public class DepositRewardSetting
{
	public decimal BonusAmount { get; set; }

	public decimal DepositMinimumLimit { get; set; }

	public decimal Percentage { get; set; }

	public decimal TurnoverMultiple { get; set; }
}
