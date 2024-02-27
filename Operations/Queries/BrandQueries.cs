using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TGS.Core.DataServices.Base.Extensions;
using TGS.Core.DataServices.Tgs;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Generics;
using TGS.Core.Models.Services.Caches;
using TGS.Core.Models.Services.Data;

namespace TGS.Services.Operations.Queries;

public interface IBrandQueries
{
	Task<Dictionary<string, string>> GetBrandCurrenciesByExternalCodeAsync(Guid brandId);

	Task<Dictionary<string, string>> GetBrandCurrenciesByInternalCodeAsync(Guid brandId);

	Task<int?> GetBrandDefaultBetLimitAsync(Guid brandId, string currencyCode);

	Task<CachedBrandGameProvider[]> GetBrandGameProvidersAsync(Guid brandId);

	Task<CachedBrandGame[]> GetBrandGamesAsync(Guid brandId);

	Task<Guid> GetBrandIdByApiKeyAsync(string apiKey);

	Task<Guid> GetBrandIdByClientIdAsync(string clientId);

	Task<Guid> GetBrandIdByCodeAsync(string brandCode);

	Task<Dictionary<string, string>> GetBrandLanguagesByExternalCodeAsync(Guid brandId);

	Task<Dictionary<string, string>> GetBrandLanguagesByInternalCodeAsync(Guid brandId);

	Task<CachedBrand> GetCachedBrandAsync(Guid brandId);

	Task<GetCachedBrandGameProviderConfigurationResult> GetCachedBrandGameProviderConfigurationAsync(
		Guid brandId, Guid gameProviderId);

	Task<CachedAnnouncement[]> GetEffectiveAnnouncementsAsync(Guid brandId);
}

public sealed class BrandQueries(ITgsRepository repository) : IBrandQueries
{
	async Task<Dictionary<string, string>> IBrandQueries.GetBrandCurrenciesByExternalCodeAsync(
		Guid brandId)
	{
		const string spName = "dbo.brand_get_currencies_by_external_code";
		var r = await repository.SqlQueryMultipleAsync<Map<string, string>>(spName,
			parameters: new Dictionary<string, object>
			{
				{":brand_id", brandId}
			});

		return r.ToDictionary(m => m.Key, m => m.Value, StringComparer.OrdinalIgnoreCase);
	}

	async Task<Dictionary<string, string>> IBrandQueries.GetBrandCurrenciesByInternalCodeAsync(
		Guid brandId)
	{
		const string spName = "dbo.brand_get_currencies_by_internal_code";
		var r = await repository.SqlQueryMultipleAsync<Map<string, string>>(spName,
			parameters: new Dictionary<string, object>
			{
				{":brand_id", brandId}
			});

		return r.ToDictionary(m => m.Key, m => m.Value, StringComparer.OrdinalIgnoreCase);
	}

	Task<int?> IBrandQueries.GetBrandDefaultBetLimitAsync(Guid brandId, string currencyCode) =>
		repository.BrandBetLimits.Where(bl =>
				bl.BrandId == brandId && bl.BetLimit.CurrencyCode == currencyCode && bl.IsDefault)
			.Select(bl => (int?)bl.BetLimitId).FirstOrDefaultAsync();

	async Task<CachedBrandGameProvider[]> IBrandQueries.GetBrandGameProvidersAsync(Guid brandId)
	{
		const string spName = "dbo.brand_get_brand_game_providers_for_cache";
		var r = await repository.SqlQueryMultipleAsync<CachedBrandGameProvider>(spName,
			parameters: new Dictionary<string, object>
			{
				{":brand_id", brandId}
			});

		return r.ToArray();
	}

	async Task<CachedBrandGame[]> IBrandQueries.GetBrandGamesAsync(Guid brandId)
	{
		const string spName = "dbo.brand_get_brand_games_for_cache";
		var r = await repository.SqlQueryMultipleAsync<CachedBrandGame>(spName,
			parameters: new Dictionary<string, object>
			{
				{":brand_id", brandId}
			});

		return r.ToArray();
	}

	Task<Guid> IBrandQueries.GetBrandIdByApiKeyAsync(string apiKey) =>
		repository.Brands.Where(b => b.ApiKey == apiKey)
			.Select(b => b.Id).FirstOrDefaultAsync();

	Task<Guid> IBrandQueries.GetBrandIdByClientIdAsync(string clientId) =>
		repository.Brands.Where(b => b.ClientId == clientId)
			.Select(b => b.Id).FirstOrDefaultAsync();

	Task<Guid> IBrandQueries.GetBrandIdByCodeAsync(string brandCode) =>
		repository.Brands.Where(b => b.Code == brandCode)
			.Select(b => b.Id).FirstOrDefaultAsync();

	async Task<Dictionary<string, string>> IBrandQueries.GetBrandLanguagesByExternalCodeAsync(Guid brandId)
	{
		const string spName = "dbo.brand_get_languages_by_external_code";
		var r = await repository.SqlQueryMultipleAsync<Map<string, string>>(spName,
			parameters: new Dictionary<string, object>
			{
				{":brand_id", brandId}
			});

		return r.ToDictionary(m => m.Key, m => m.Value, StringComparer.OrdinalIgnoreCase);
	}

	async Task<Dictionary<string, string>> IBrandQueries.GetBrandLanguagesByInternalCodeAsync(Guid brandId)
	{
		const string spName = "dbo.brand_get_languages_by_internal_code";
		var r = await repository.SqlQueryMultipleAsync<Map<string, string>>(spName,
			parameters: new Dictionary<string, object>
			{
				{":brand_id", brandId}
			});

		return r.ToDictionary(m => m.Key, m => m.Value, StringComparer.OrdinalIgnoreCase);
	}

	Task<CachedBrand> IBrandQueries.GetCachedBrandAsync(Guid brandId)
	{
		const string sql = "dbo.brand_get_brand_for_cache";
		return repository.SqlQuerySingleAsync<CachedBrand>(sql, parameters: new Dictionary<string, object>
		{
			{":brand_id", brandId}
		});
	}

	Task<GetCachedBrandGameProviderConfigurationResult> IBrandQueries.
		GetCachedBrandGameProviderConfigurationAsync(Guid brandId, Guid gameProviderId)
	{
		const string spName = "dbo.brand_get_brand_game_provider_configuration_for_cache";
		return repository.SqlQuerySingleAsync<GetCachedBrandGameProviderConfigurationResult>(spName, parameters:
			new Dictionary<string, object>
			{
				{":brand_id", brandId},
				{":game_provider_id", gameProviderId}
			});
	}

	Task<CachedAnnouncement[]> IBrandQueries.GetEffectiveAnnouncementsAsync(Guid brandId)
	{
		var now = DateTimeOffset.UtcNow;

		return repository.Announcements
			.Where(a => a.BrandId == brandId &&
				a.Status == AnnounceStatus.Effective &&
				now >= a.From && now < a.To)
			.Select(a => new CachedAnnouncement
			{
				Id = a.Id,
				Subject = a.Subject,
				Content = a.Content,
				From = a.From,
				To = a.To,
				LanguageContent = a.Localization
			}).ToArrayAsync();
	}
}