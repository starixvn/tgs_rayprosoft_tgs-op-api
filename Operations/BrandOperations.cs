using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TGS.Core.Infrastructures.Json;
using TGS.Core.Infrastructures.Redis;
using TGS.Core.Models;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Services.Caches;
using TGS.Services.Operations.Queries;

namespace TGS.Services.Operations;

public sealed class BrandOperations : IBrandOperations
{
	private readonly IBrandQueries _brandQueries;
	private readonly IRedisCacheManager _cacheManager;
	private readonly IBrandOperations _interface;
	private readonly IJsonSerializationProvider _json;
	private readonly ILogger<BrandOperations> _logger;

	public BrandOperations(
		IRedisCacheManager cacheManager,
		IBrandQueries brandQueries,
		IJsonSerializationProvider json,
		ILogger<BrandOperations> logger)
	{
		_cacheManager = cacheManager;
		_brandQueries = brandQueries;
		_json = json;
		_logger = logger;

		_interface = this;
	}

	Task<string> IBrandOperations.GetBrandCodeByIdAsync(Guid brandId)
		=> GetCachedBrandValueAsync(brandId,
			CachedBrandFields.BrandCode,
			v => (string)v,
			brand => brand?.Code);

	async Task<int> IBrandOperations.GetBrandDefaultBetLimitAsync(Guid brandId, string currencyCode)
	{
		const RedisCacheType type = RedisCacheType.BrandDefaultBetLimit;
		var value = await _cacheManager.SafeGetHashSetAsync(type, brandId, currencyCode);

		if (value.HasValue)
		{
			return (int)value;
		}

		var betLimitId = await _brandQueries.GetBrandDefaultBetLimitAsync(brandId, currencyCode);

		if (!betLimitId.HasValue)
		{
			throw new InvalidOperationException(
				$"Brand with id '{brandId}' doesn't have default bet limit for currency '{currencyCode}'");
		}

		await _cacheManager.SafeCacheHashSetAsync(type,
			brandId,
			[
				new(currencyCode, betLimitId.Value)
			],
			TimeSpan.FromHours(2));

		return betLimitId.Value;
	}

	async Task<CachedBrandGame> IBrandOperations.GetBrandGameAsync(Guid brandId, Guid gameProviderId, int gameId)
	{
		var games = await GetAndCachedBrandGamesAsync(brandId);

		return games.FirstOrDefault(g => g.GameProviderId == gameProviderId && g.GameId == gameId);
	}

	async Task<CachedBrandGameProvider> IBrandOperations.GetBrandGameProviderAsync(
		Guid brandId,
		Guid gameProviderId)
	{
		var gameProviders = await _interface.GetBrandGameProvidersAsync(brandId);

		return gameProviders.FirstOrDefault(bgp => bgp.GameProviderId == gameProviderId);
	}

	async Task<CachedBrandGameProviderConfiguration> IBrandOperations.
		GetBrandGameProviderConfigurationAsync(Guid brandId, Guid gameProviderId)
	{
		var bgpc = await GetCachedBrandGameProviderConfigurationOrNullAsync(brandId, gameProviderId);

		if (bgpc != null)
		{
			return bgpc;
		}

		var r = await _brandQueries.GetCachedBrandGameProviderConfigurationAsync(brandId, gameProviderId)
			?? throw new InvalidOperationException(
				$"BrandGameProviderConfiguration doesn't exist with brandId: {brandId} and gameProviderId: {gameProviderId}");

		bgpc = new()
		{
			GameProviderId = gameProviderId,
			GameProviderConfigurationId = r.GameProviderConfigurationId,
			DefaultLanguageCode = r.DefaultLanguageCode,
			QueryStrings = new()
			{
				Desktop = r.DesktopQueryString,
				Mobile = r.MobileQueryString
			}
		};

		await CacheBrandGameProviderConfigurationAsync(brandId, gameProviderId, bgpc);

		return bgpc;
	}

	async Task<string> IBrandOperations.GetBrandGameProviderConfigurationDefaultLanguageAsync(
		Guid brandId,
		Guid gameProviderId)
	{
		var bgpc = await _interface.GetBrandGameProviderConfigurationAsync(brandId, gameProviderId);

		return bgpc.DefaultLanguageCode;
	}

	async Task<Guid> IBrandOperations.GetBrandGameProviderConfigurationIdAsync(
		Guid brandId,
		Guid gameProviderId)
	{
		var bgpc = await _interface.GetBrandGameProviderConfigurationAsync(brandId, gameProviderId);

		return bgpc.GameProviderConfigurationId;
	}

	Task<CachedBrandGameProvider[]> IBrandOperations.GetBrandGameProvidersAsync(Guid brandId) =>
		_cacheManager.SafeGetAndCacheAsync(RedisCacheType.BrandGameProviders,
			brandId,
			TimeSpan.FromHours(2),
			() => _brandQueries.GetBrandGameProvidersAsync(brandId));

	async Task<Status?> IBrandOperations.GetBrandGameProviderStatusAsync(Guid brandId, Guid gameProviderId)
	{
		var gameProvider = await _interface.GetBrandGameProviderAsync(brandId, gameProviderId);

		return gameProvider?.BrandGameProviderStatus;
	}

	async Task<CachedBrandGame[]> IBrandOperations.GetBrandGamesAsync(
		Guid brandId, Guid gameProviderId,
		GamePlatformType[] platformTypes)
	{
		var games = await GetAndCachedBrandGamesAsync(brandId);
		var types = platformTypes?.Length > 0 ? platformTypes : Consts.AllGamePlatformTypes;

		return games.Where(g => g.GameProviderId == gameProviderId &&
			types.Contains(g.PlatformType)).ToArray();
	}

	async Task<CachedBrandGame[]> IBrandOperations.GetBrandGamesAsync(
		Guid brandId,
		GamePlatformType[] platformTypes)
	{
		var games = await GetAndCachedBrandGamesAsync(brandId);
		var types = platformTypes?.Length > 0 ? platformTypes : Consts.AllGamePlatformTypes;

		return games.Where(g => types.Contains(g.PlatformType)).ToArray();
	}

	async Task<GameStatus?> IBrandOperations.GetBrandGameStatusAsync(Guid brandId, Guid gameProviderId, int gameId)
	{
		var game = await _interface.GetBrandGameAsync(brandId, gameProviderId, gameId);

		return game?.BrandGameStatus;
	}

	Task<Guid> IBrandOperations.GetBrandIdByApiKeyAsync(string apiKey) =>
		_cacheManager.SafeGetAndCacheAsync(RedisCacheType.Brand,
			apiKey,
			TimeSpan.FromHours(2),
			() => _brandQueries.GetBrandIdByApiKeyAsync(apiKey));

	Task<Guid> IBrandOperations.GetBrandIdByClientIdAsync(string clientId) =>
		_cacheManager.SafeGetAndCacheAsync(RedisCacheType.Brand,
			clientId,
			TimeSpan.FromHours(2),
			() => _brandQueries.GetBrandIdByClientIdAsync(clientId));

	Task<Guid> IBrandOperations.GetBrandIdByCodeAsync(string brandCode) =>
		_cacheManager.SafeGetAndCacheAsync(RedisCacheType.Brand,
			brandCode,
			TimeSpan.FromHours(2),
			() => _brandQueries.GetBrandIdByCodeAsync(brandCode));

	Task<string> IBrandOperations.GetBrandSecretKeyAsync(Guid brandId) =>
		GetCachedBrandValueAsync(brandId,
			CachedBrandFields.SecretKey,
			v => (string)v,
			brand => brand?.SecretKey);

	Task<Status?> IBrandOperations.GetBrandStatusAsync(Guid brandId) =>
		GetCachedBrandValueAsync(brandId,
			CachedBrandFields.Status,
			v => (Status)(int)v,
			brand => brand?.Status);

	async Task<CachedBrand> IBrandOperations.GetCachedBrandAsync(Guid brandId)
	{
		var brand = await GetCachedBrandOrNullAsync(brandId);

		if (brand != null)
		{
			return brand;
		}

		brand = await _brandQueries.GetCachedBrandAsync(brandId);

		if (brand == null)
		{
			throw new InvalidOperationException($"Brand not found with id: {brandId}");
		}

		await CacheBrandAsync(brandId, brand);

		return brand;
	}

	Task<CachedAnnouncement[]> IBrandOperations.GetEffectiveAnnouncementsAsync(Guid brandId) =>
		_cacheManager.SafeGetAndCacheAsync(RedisCacheType.Announcements,
			brandId,
			TimeSpan.FromMinutes(10),
			() => _brandQueries.GetEffectiveAnnouncementsAsync(brandId));

	Task<string> IBrandOperations.GetTimeZoneByBrandIdAsync(Guid brandId) =>
			GetCachedBrandValueAsync(brandId,
			CachedBrandFields.TimeZoneId,
			v => (string)v,
			brand => brand?.TimeZoneId);

	#region Private Methods

	private async Task CacheBrandAsync(Guid brandId, CachedBrand brand)
	{
		try
		{
			var fields = new HashEntry[]
			{
				new(CachedBrandFields.BrandCode, brand.Code),
				new(CachedBrandFields.BrandName, brand.Name),
				new(CachedBrandFields.TimeZoneId, brand.TimeZoneId ?? string.Empty),
				new(CachedBrandFields.Status, (int)brand.Status),
				new(CachedBrandFields.ResellerId, brand.ResellerId.ToString()),
				new(CachedBrandFields.ResellerCode, brand.ResellerCode),
				new(CachedBrandFields.LicenseeId, brand.LicenseeId.ToString()),
				new(CachedBrandFields.LicenseeCode, brand.LicenseeCode),
				new(CachedBrandFields.ClientId, brand.ClientId),
				new(CachedBrandFields.SecretKey, brand.SecretKey)
			};

			await _cacheManager.SafeCacheHashSetAsync(RedisCacheType.Brand,
				brandId,
				fields,
				TimeSpan.FromHours(2),
				true);
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Failed to cache brand for brandId: {brandId}", brandId);
		}
	}

	private async Task CacheBrandGameProviderConfigurationAsync(
		Guid brandId, Guid gameProviderId,
		CachedBrandGameProviderConfiguration configuration)
	{
		try
		{
			var fields = new HashEntry[]
			{
				new(gameProviderId.ToString(), _json.SerializeToString(configuration))
			};

			await _cacheManager.SafeCacheHashSetAsync(RedisCacheType.BrandGameProviderConfiguration,
				brandId,
				fields,
				TimeSpan.FromHours(2),
				true);
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Failed to cache brand game provider configuration for brandId: {brandId}", brandId);
		}
	}

	private Task<CachedBrandGame[]> GetAndCachedBrandGamesAsync(Guid brandId) =>
		_cacheManager.SafeGetAndCacheAsync(RedisCacheType.BrandGames,
			brandId,
			TimeSpan.FromHours(2),
			() => _brandQueries.GetBrandGamesAsync(brandId));

	private async Task<CachedBrandGameProviderConfiguration>
		GetCachedBrandGameProviderConfigurationOrNullAsync(Guid brandId, Guid gameProviderId)
	{
		try
		{
			var data = await _cacheManager.SafeGetHashSetAsync(RedisCacheType.BrandGameProviderConfiguration,
				brandId,
				gameProviderId.ToString());

			if (data.HasValue)
			{
				return _json.DeserializeFromString<CachedBrandGameProviderConfiguration>(data);
			}
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Failed to get cached brand game provider configuration for brandId: {brandId}",
				brandId);
		}

		return null;
	}

	private async Task<CachedBrand> GetCachedBrandOrNullAsync(Guid brandId)
	{
		try
		{
			var values = await _cacheManager.SafeGetHashSetAsync(RedisCacheType.Brand,
				brandId,
				[
					CachedBrandFields.BrandCode,
					CachedBrandFields.BrandName,
					CachedBrandFields.TimeZoneId,
					CachedBrandFields.Status,
					CachedBrandFields.ResellerId,
					CachedBrandFields.ResellerCode,
					CachedBrandFields.LicenseeId,
					CachedBrandFields.LicenseeCode,
					CachedBrandFields.ClientId,
					CachedBrandFields.SecretKey
				]);

			if (values.All(a => !a.IsNull))
			{
				return new()
				{
					Code = values[0],
					Name = values[1],
					TimeZoneId = values[2],
					Status = (Status)(int)values[3],
					ResellerId = Guid.Parse(values[4]),
					ResellerCode = values[5],
					LicenseeId = Guid.Parse(values[6]),
					LicenseeCode = values[7],
					ClientId = values[8],
					SecretKey = values[9]
				};
			}
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Failed to get cached brand for brandId: {brandId}", brandId);
		}

		return null;
	}

	private async Task<T> GetCachedBrandValueAsync<T>(
		Guid brandId, string field, Func<RedisValue, T> cached,
		Func<CachedBrand, T> noCache)
	{
		var value = await _cacheManager.SafeGetHashSetAsync(RedisCacheType.Brand, brandId, field);

		if (value.HasValue)
		{
			return cached(value);
		}

		var brand = await _interface.GetCachedBrandAsync(brandId);

		return noCache(brand);
	}

	private static class CachedBrandFields
	{
		public const string BrandCode = "brandCode";
		public const string BrandName = "brandName";
		public const string ClientId = "clientId";
		public const string LicenseeCode = "licenseeCode";
		public const string LicenseeId = "licenseeId";
		public const string ResellerCode = "resellerCode";
		public const string ResellerId = "resellerId";
		public const string SecretKey = "secretKey";
		public const string Status = "status";
		public const string TimeZoneId = "timezoneId";
	}

	#endregion Private Methods
}
