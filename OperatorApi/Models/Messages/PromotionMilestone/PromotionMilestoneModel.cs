#nullable enable
namespace TGS.Apis.OperatorApi.Models.Messages.PromotionMilestone;

public class PromotionMilestoneModel
{
	public Guid PromotionId { get; set; }
	public string Name { get; set; } = default!;
	public string? SubSubject { get; set; }
	public decimal TotalValidBetAmount { get; init; }
	public decimal TotalDepositAmount { get; init; }
	public DateTime From { get; init; }
	public DateTime To { get; init; }
	public required IEnumerable<MilestoneCondition> Conditions { get; set; }
}