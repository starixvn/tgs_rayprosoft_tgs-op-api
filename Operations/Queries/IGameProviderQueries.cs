using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TGS.Core.Models.Services.Caches;
using TGS.Core.Models.Services.Data;

namespace TGS.Services.Operations.Queries;

public interface IGameProviderQueries
{
	Task<GetCachedGameProviderResult> GetCachedGameProviderAsync(Guid gameProviderId);

	Task<GetCachedGameProviderConfigurationResult> GetCachedGameProviderConfigurationAsync(
		Guid gameProviderConfigurationId);

	Task<CachedGame[]> GetCachedGamesAsync(Guid gameProviderId);

	Task<Dictionary<string, string>> GetGameProviderCurrenciesByExternalCodeAsync(Guid gameProviderId);

	Task<Dictionary<string, string>> GetGameProviderCurrenciesByInternalCodeAsync(Guid gameProviderId);

	Task<Guid> GetGameProviderIdByCodeAsync(string gameProviderCode);

	Task<IdAndCodeAndConfigurationId> GetGameProviderIdsByCredentialsAsync(string clientId, string secretKey);

	Task<Dictionary<string, string>> GetGameProviderLanguagesByExternalCodeAsync(Guid gameProviderId);

	Task<Dictionary<string, string>> GetGameProviderLanguagesByInternalCodeAsync(Guid gameProviderId);

	Task<Dictionary<Guid, string>> GetGameProvidersLocalizationByGameProviderIdsAsync(Guid[] gameProviderIds);

	Task<CachedSubGame[]> GetSubGamesAsync(Guid gameProviderId);
}
