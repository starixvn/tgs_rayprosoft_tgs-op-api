using System;
using System.Threading.Tasks;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Services.Caches;

namespace TGS.Services.Operations;

public interface IBrandOperations
{
	Task<string> GetBrandCodeByIdAsync(Guid brandId);

	[Obsolete("淘汰")]
	Task<int> GetBrandDefaultBetLimitAsync(Guid brandId, string currencyCode);

	Task<CachedBrandGame> GetBrandGameAsync(Guid brandId, Guid gameProviderId, int gameId);

	Task<CachedBrandGameProvider> GetBrandGameProviderAsync(Guid brandId, Guid gameProviderId);

	Task<CachedBrandGameProviderConfiguration> GetBrandGameProviderConfigurationAsync(
		Guid brandId,
		Guid gameProviderId);

	Task<string> GetBrandGameProviderConfigurationDefaultLanguageAsync(Guid brandId, Guid gameProviderId);

	Task<Guid> GetBrandGameProviderConfigurationIdAsync(Guid brandId, Guid gameProviderId);

	Task<CachedBrandGameProvider[]> GetBrandGameProvidersAsync(Guid brandId);

	Task<Status?> GetBrandGameProviderStatusAsync(Guid brandId, Guid gameProviderId);

	Task<CachedBrandGame[]> GetBrandGamesAsync(Guid brandId, GamePlatformType[] platformTypes = null);

	Task<CachedBrandGame[]> GetBrandGamesAsync(
		Guid brandId, Guid gameProviderId,
		GamePlatformType[] platformTypes = null);

	Task<GameStatus?> GetBrandGameStatusAsync(Guid brandId, Guid gameProviderId, int gameId);

	Task<Guid> GetBrandIdByApiKeyAsync(string apiKey);

	Task<Guid> GetBrandIdByClientIdAsync(string clientId);

	Task<Guid> GetBrandIdByCodeAsync(string brandCode);

	Task<string> GetBrandSecretKeyAsync(Guid brandId);

	Task<Status?> GetBrandStatusAsync(Guid brandId);

	Task<CachedBrand> GetCachedBrandAsync(Guid brandId);

	Task<CachedAnnouncement[]> GetEffectiveAnnouncementsAsync(Guid brandId);

	Task<string> GetTimeZoneByBrandIdAsync(Guid brandId);
}