using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TGS.Core.Models.Enums;
using Tgs.Report.Client;
using Tgs.Report.Client.Models.Round;
using TGS.Services.Operator.GameProviders;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.PlayerReport;

internal class GetPlayerRoundsQueryService(
	IGameProviderRepository gameProviderRepository,
	IReportClient reportClient)
	: IGetService<GetPlayerRoundsQuery, RoundsResponse>
{
	public async ValueTask<RoundsResponse> GetAsync(GetPlayerRoundsQuery query,
		CancellationToken cancellationToken = default)
	{
		var roundsRequest = new GetRoundsRequest
		{
			BrandId = query.BrandId,
			PlayerCode = query.PlayerCode,
			From = query.From,
			To = query.To,
			Page = query.Page,
			Rows = query.Rows,
			OrderBy = query.OrderBy,
			SortBy = query.SortBy,
			ShowAll = query.ShowAll
		};

		var response = await reportClient.GetRoundsAsync(roundsRequest).ConfigureAwait(false);

		var roundsTotalRequest = new GetRoundsTotalRequest
		{
			BrandId = query.BrandId, 
			PlayerCode = query.PlayerCode, 
			From = query.From, 
			To = query.To
		};

		var totalResponse = await reportClient.GetRoundsTotalAsync(roundsTotalRequest);

		var gameIds = response.Items.Select(x => x.GameId);

		var games = await gameProviderRepository.QueryAllGamesAsync(query.BrandId)
			.Where(x => gameIds.Contains(x.Id)).Select(
				x => new
				{
					GameId = x.Id,
					Localization = JsonSerializer
						.Deserialize<IDictionary<string, GameLocalizationData>>(x.Localization)
				}).ToDictionaryAsync(x => x.GameId, cancellationToken)
			.ConfigureAwait(false);

		return new RoundsResponse
		{
			Items = response.Items.Select(model =>
			{
				var game = games.FirstOrDefault(g => g.Key == model.GameId);

				var gameName = game.Value.Localization?.TryGetValue(query.CultureName, out var localization) == true
					? localization.Name
					: null;

				return new RoundModel
				{
					Id = model.Id,
					AdditionalData = model.AdditionalData,
					BetAmount = model.BetAmount,
					ClosedOn = model.ClosedOn,
					Commission = model.Commission,
					CreatedOn = model.CreatedOn,
					CurrencyCode = model.CurrencyCode,
					ExternalClosedOn = model.ExternalClosedOn,
					ExternalCreatedOn = model.ExternalCreatedOn,
					ExternalPlayerCode = model.ExternalPlayerCode,
					ExternalRoundId = model.ExternalRoundId,
					GameId = model.GameId,
					GameName = gameName ?? model.GameName,
					GameProviderCode = model.GameProviderCode,
					GameProviderName = model.GameProviderName,
					GameProviderId = model.GameProviderId,
					PlayerCode = model.PlayerCode,
					PlatformType = (PlatformType)model.PlatformType,
					PlayerName = model.PlayerName,
					SettleAmount = model.SettleAmount,
					Status = (ClientRoundStatus)model.Status,
					SubGameProviderName = model.SubGameProviderName,
					TieBetAdjustment = model.TieBetAdjustment,
					UpdatedOn = model.UpdatedOn,
					ValidBetAmount = model.ValidBetAmount,
					Turnover = model.Turnover,
					WinLoss = model.WinLoss,
					EffectiveBettingPercentage = model.EffectiveBettingPercentage,
					OriginalTurnover = model.OriginalTurnover
				};
			}),
			Total = new RoundTotalModel
			{
				BetAmount = totalResponse.BetAmount ?? 0M,
				ValidBetAmount = totalResponse.ValidBetAmount ?? 0M,
				WinLoss = totalResponse.WinLoss ?? 0M
			},
			TotalCount = response.TotalCount
		};
	}
}