using System.Text.Json;

#nullable enable

namespace TGS.Apis.OperatorApi.Models.Messages;

public class PromotionViewModel
{
	public JsonDocument CampaignDetail { get; init; } = default!;

	public string? Content { get; init; }

	public string? DesktopBanner { get; init; }

	public DateTime From { get; init; }

	public Guid Id { get; init; }

	public JsonDocument Localization { get; init; } = default!;

	public string? MobileBanner { get; init; }

	public string Name { get; init; } = default!;

	public int Order { get; init; }

	public string PromotionType { get; init; } = default!;

	public string? SubSubject { get; init; }

	public string? Terms { get; init; }

	public DateTime To { get; init; }

	public IEnumerable<int> VipLevels { get; init; } = [];
}