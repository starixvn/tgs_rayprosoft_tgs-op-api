using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tgs.Promotion.Clients;
using Tgs.Promotion.Clients.Models.Promotion;

namespace TGS.Services.Operator.PlayerRegisterFlow;

internal class SetPromotionJoinRegisterFlowStep : IRegisterFlowStep
{
	private readonly ILogger<SetPromotionJoinRegisterFlowStep> _logger;
	private readonly IPromotionClient _promotionClient;
	private Guid[]? _promotionIds;

	public string Type => "JoinPromotion";

	public SetPromotionJoinRegisterFlowStep(
		IPromotionClient promotionClient,
		ILogger<SetPromotionJoinRegisterFlowStep> logger)
	{
		_promotionClient = promotionClient ?? throw new ArgumentNullException(nameof(promotionClient));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public ValueTask<bool> ConfirmCompletedAsync(string? playerCode, CancellationToken cancellationToken = default)
	{
		return ValueTask.FromResult(!string.IsNullOrEmpty(playerCode));
	}

	public async ValueTask<RegisterFlowProcessResult> ProcessInputAsync(
		Guid brandId,
		JsonDocument input,
		PlayerLocationInfo locationInfo,
		CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("SetPromotionJoinRegisterFlowStep input:{input}", input);
		_logger.LogInformation("SetPromotionJoinRegisterFlowStep locationInfo:{locationInfo}", locationInfo);
		if (string.IsNullOrWhiteSpace(locationInfo.PlayerCode))
			return new RegisterFlowProcessResult(false, null, null);

		_logger.LogInformation("SetPromotionJoinRegisterFlowStep promotionIds:{promotionIds}", _promotionIds);
		foreach (var promotionId in _promotionIds!)
		{
			_ = await _promotionClient
				.JoinAsync(new JoinPromotionRequest(promotionId, locationInfo.PlayerCode))
				.ConfigureAwait(false);

			_logger.LogInformation("SetPromotionJoinRegisterFlowStep join promotion, promotion Id:{promotionId}, playerCode:{locationInfo.PlayerCode}", promotionId, locationInfo.PlayerCode);
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
			_promotionIds = Array.Empty<Guid>();
	}
}
