using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tgs.Promotion.Clients;
using Tgs.Promotion.Clients.Models.Promotion;

namespace TGS.Services.Operator.PlayerRegisterFlow;

internal class SetPromotionJoinRegisterFlowStep(
	IPromotionClient promotionClient,
	ILogger<SetPromotionJoinRegisterFlowStep> logger) : IRegisterFlowStep
{
	private readonly ILogger<SetPromotionJoinRegisterFlowStep> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly IPromotionClient _promotionClient = promotionClient ?? throw new ArgumentNullException(nameof(promotionClient));
	private Guid[]? _promotionIds;

	public bool IsAuto => true;

	public string Type => "JoinPromotion";

	public ValueTask<bool> ConfirmCompletedAsync(string? playerCode, CancellationToken cancellationToken = default)
	{
		return ValueTask.FromResult(false);
	}

	public async ValueTask<RegisterFlowProcessResult> ProcessInputAsync(
		Guid brandId,
		JsonDocument input,
		PlayerLocationInfo locationInfo,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(locationInfo.PlayerCode))
			return new RegisterFlowProcessResult(false, null, null);

		foreach (var promotionId in _promotionIds!)
		{
			_ = await _promotionClient
				.JoinAsync(new JoinPromotionRequest(promotionId, locationInfo.PlayerCode))
				.ConfigureAwait(false);

			_logger.LogInformation(
				"SetPromotionJoinRegisterFlowStep join promotion, "
					+ "promotion Id:{promotionId}, playerCode:{locationInfo.PlayerCode}",
				promotionId,
				locationInfo.PlayerCode);
		}

		return new RegisterFlowProcessResult(true, null, null);
	}

	public void SetupStep(JsonElement jsonElement)
	{
		if (jsonElement.TryGetProperty("promotionIds", out var prop))
		{
			_logger.LogInformation("SetPromotionJoinRegisterFlowStep prop:{prop.GetRawText()}", prop.GetRawText());
			_promotionIds = JsonSerializer.Deserialize<Guid[]>(prop.GetRawText())!;
		}
		else
			_promotionIds = [];
	}
}