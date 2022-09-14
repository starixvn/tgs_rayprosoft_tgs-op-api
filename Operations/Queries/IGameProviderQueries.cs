using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TGS.Core.Models.Services.Caches;
using TGS.Core.Models.Services.Data;

namespace TGS.Services.Operations.Queries;

public interface IGameProviderQueries
{
    Task<Guid> GetGameProviderIdByCodeAsync(string gameProviderCode);

    Task<Dictionary<string, string>> GetGameProviderCurrenciesByExternalCodeAsync(Guid gameProviderId);

    Task<Dictionary<string, string>> GetGameProviderCurrenciesByInternalCodeAsync(Guid gameProviderId);

    Task<GetCachedGameProviderResult> GetCachedGameProviderAsync(Guid gameProviderId);

    Task<GetCachedGameProviderConfigurationResult> GetCachedGameProviderConfigurationAsync(
        Guid gameProviderConfigurationId);

    Task<Dictionary<string, string>> GetGameProviderLanguagesByExternalCodeAsync(Guid gameProviderId);

    Task<Dictionary<string, string>> GetGameProviderLanguagesByInternalCodeAsync(Guid gameProviderId);

    Task<CachedGame[]> GetCachedGamesAsync(Guid gameProviderId);

    Task<CachedSubGame[]> GetSubGamesAsync(Guid gameProviderId);

    Task<IdAndCodeAndConfigurationId> GetGameProviderIdsByCredentialsAsync(string clientId, string secretKey);

    Task<Dictionary<Guid, string>> GetGameProvidersLocalizationByGameProviderIdsAsync(Guid[] gameProviderIds);
}
