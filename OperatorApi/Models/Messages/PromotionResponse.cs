using System.Text.Json;
using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class PromotionResponse
{
	public decimal AmountUsed { get; init; }

	public bool AutoSentPlayerBonus { get; init; }

	public decimal BonusAmountLimit { get; init; }

	public Guid BrandId { get; init; }

	public string Content { get; init; }

	public string CreatedByUsername { get; init; }

	public DateTimeOffset CreatedOn { get; init; }

	public string DesktopBanner { get; init; }

	public DateTimeOffset From { get; init; }

	public Guid[] GameProviderIds { get; init; }

	public GameType? GameType { get; init; }

	public Guid Id { get; init; }

	public JsonDocument Localization { get; init; }

	public string MobileBanner { get; init; }

	public string Name { get; init; }

	public decimal Percentage { get; init; }

	public string PromotionType { get; init; }

	public SwitchStatus Status { get; init; }

	public string SubSubject { get; init; }

	public string Terms { get; init; }

	public DateTimeOffset To { get; init; }

	public decimal TotalAmountLimit { get; init; }

	public decimal TurnoverMultiple { get; init; }

	public bool Unlimited { get; init; }

	public string UpdatedByUsername { get; init; }

	public DateTimeOffset UpdatedOn { get; init; }

	public int[] VipLevels { get; init; }
}