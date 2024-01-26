using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TGS.Core.DataServices.Base.Extensions;
using TGS.Core.DataServices.Tgs;
using TGS.Core.Infrastructures.Extensions;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Generics;
using TGS.Core.Models.Services.Caches;
using TGS.Core.Models.Services.Data;

namespace TGS.Services.Operations.Queries;

public sealed class GameProviderQueries(ITgsRepository repository) : IGameProviderQueries
{
	Task<GetCachedGameProviderResult> IGameProviderQueries.GetCachedGameProviderAsync(Guid gameProviderId)
	{
		const string spName = "dbo.game_provider_get_game_provider_for_cache";
		return repository.SqlQuerySingleAsync<GetCachedGameProviderResult>(spName,
			parameters: new Dictionary<string, object> { { ":game_provider_id", gameProviderId } });
	}

	Task<GetCachedGameProviderConfigurationResult> IGameProviderQueries.GetCachedGameProviderConfigurationAsync(
		Guid gameProviderConfigurationId)
	{
		const string spName = "dbo.game_provider_get_game_provider_configuration_for_cache";
		return repository.SqlQuerySingleAsync<GetCachedGameProviderConfigurationResult>(spName,
			parameters: new Dictionary<string, object>
			{
				{":game_provider_configuration_id", gameProviderConfigurationId}
			});
	}

	async Task<CachedGame[]> IGameProviderQueries.GetCachedGamesAsync(Guid gameProviderId)
	{
		const string spName = "dbo.game_provider_get_games_for_cache";
		var r = await repository.SqlQueryMultipleAsync<GetCachedGameResult>(spName,
			parameters: new Dictionary<string, object> { { ":game_provider_id", gameProviderId } });

		return r.Select(g => new CachedGame
		{
			Id = g.Id,
			Code = g.Code,
			Name = g.Name,
			Description = g.Description,
			Status = g.Status,
			ExternalId = g.ExternalId,
			SubGame = g.SubGame,
			AvailableToBrand = g.AvailableToBrand,
			RuleUrl = g.RuleUrl,
			EndpointPath = g.EndpointPath,
			Type = g.Type,
			PlatformType = g.PlatformType,
			HasTurnoverOnTieBet = g.HasTurnoverOnTieBet,
			AvailableCurrencies = g.AvailableCurrencies.IsNotNullOrEmpty()
				? g.AvailableCurrencies.SplitByComma()
				: []
		}).ToArray();
	}

	async Task<Dictionary<string, string>> IGameProviderQueries.GetGameProviderCurrenciesByExternalCodeAsync(
		Guid gameProviderId)
	{
		const string spName = "dbo.game_provider_get_currencies_by_external_code";
		var r = await repository.SqlQueryMultipleAsync<Map<string, string>>(spName,
			parameters: new Dictionary<string, object> { { ":game_provider_id", gameProviderId } });

		return r.ToDictionary(m => m.Key, m => m.Value, StringComparer.OrdinalIgnoreCase);
	}

	async Task<Dictionary<string, string>> IGameProviderQueries.GetGameProviderCurrenciesByInternalCodeAsync(
		Guid gameProviderId)
	{
		const string spName = "dbo.game_provider_get_currencies_by_internal_code";
		var r = await repository.SqlQueryMultipleAsync<Map<string, string>>(spName,
			parameters: new Dictionary<string, object> { { ":game_provider_id", gameProviderId } });

		return r.ToDictionary(m => m.Key, m => m.Value, StringComparer.OrdinalIgnoreCase);
	}

	Task<Guid> IGameProviderQueries.GetGameProviderIdByCodeAsync(string gameProviderCode) =>
							repository.GameProviders.Where(gp => gp.Code == gameProviderCode)
			.Select(gp => gp.Id).FirstOrDefaultAsync();

	Task<IdAndCodeAndConfigurationId> IGameProviderQueries.GetGameProviderIdsByCredentialsAsync(string clientId,
		string secretKey)
	{
		var statuses = new[] { Status.Active, Status.Maintenance };

		return (from gp in repository.GameProviders
				where statuses.Contains(gp.Status)
				join gpc in repository.GameProviderConfigurations
					on gp.Id equals gpc.GameProviderId
				where gpc.ClientId == clientId &&
					gpc.SecretKey == secretKey &&
					statuses.Contains(gpc.Status)
				select new IdAndCodeAndConfigurationId
				{
					Id = gp.Id,
					Code = gp.Code,
					ConfigurationId = gpc.Id
				})
			.FirstOrDefaultAsync();
	}

	async Task<Dictionary<string, string>> IGameProviderQueries.GetGameProviderLanguagesByExternalCodeAsync(
			Guid gameProviderId)
	{
		const string spName = "dbo.game_provider_get_languages_by_external_code";
		var r = await repository.SqlQueryMultipleAsync<Map<string, string>>(spName,
			parameters: new Dictionary<string, object> { { ":game_provider_id", gameProviderId } });

		return r.ToDictionary(m => m.Key, m => m.Value, StringComparer.OrdinalIgnoreCase);
	}

	async Task<Dictionary<string, string>> IGameProviderQueries.GetGameProviderLanguagesByInternalCodeAsync(
		Guid gameProviderId)
	{
		const string spName = "dbo.game_provider_get_languages_by_internal_code";
		var r = await repository.SqlQueryMultipleAsync<Map<string, string>>(spName,
			parameters: new Dictionary<string, object> { { ":game_provider_id", gameProviderId } });

		return r.ToDictionary(m => m.Key, m => m.Value, StringComparer.OrdinalIgnoreCase);
	}

	public Task<Dictionary<Guid, string>> GetGameProvidersLocalizationByGameProviderIdsAsync(Guid[] gameProviderIds)
	{
		return repository.GameProviders
			.Where(x => gameProviderIds.Contains(x.Id)
				&& x.Localization != null)
			.ToDictionaryAsync(x => x.Id, x => x.Localization);
	}

	async Task<CachedSubGame[]> IGameProviderQueries.GetSubGamesAsync(Guid gameProviderId)
	{
		const string spName = "dbo.game_provider_get_sub_games";
		var r = await repository.SqlQueryMultipleAsync<CachedSubGame>(spName,
			parameters: new Dictionary<string, object> { { ":game_provider_id", gameProviderId } });

		return r.ToArray();
	}
}
