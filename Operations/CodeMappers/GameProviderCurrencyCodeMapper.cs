using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TGS.Core.Infrastructures.CodeMapper;
using TGS.Core.Infrastructures.Json;
using TGS.Core.Infrastructures.Redis;
using TGS.Core.Models;
using TGS.Core.Models.Enums;
using TGS.Services.Operations.Queries;

namespace TGS.Services.Operations.CodeMappers;

public interface IGameProviderCurrencyCodeMapper : ICodeMapper
{
}

public sealed class GameProviderCurrencyCodeMapper(
	IRedisCacheManager cacheManager,
	IJsonSerializationProvider json,
	IGameProviderQueries gameProviderQueries) : BaseCodeMapper(cacheManager, json), IGameProviderCurrencyCodeMapper
{
	private const int CacheForMinutes = 30;

	async Task<string> ICodeMapper.ExternalToInternalAsync(string externalCode, Guid gameProviderId)
	{
		var (found, code) = await GetMappedCodeAsync(
			externalCode,
			gameProviderId,
			RedisCacheType.Currencies,
			Consts.CachePrefix.GameProviderToTgsMap,
			"currency",
			CacheForMinutes,
			id => gameProviderQueries.GetGameProviderCurrenciesByExternalCodeAsync(id));

		return !found
			? throw new InvalidOperationException(
				$"Not found code '{externalCode}' for '{Consts.CachePrefix.GameProviderToTgsMap}' "
					+ $"used for 'currency' with filter id '{gameProviderId}' (intended to be cached as {RedisCacheType.Currencies})")
			: code;
	}

	Task<Dictionary<string, string>> ICodeMapper.GetMapByExternalCodeAsync(Guid gameProviderId) =>
		GetMappedCodesAsync(
			gameProviderId,
			RedisCacheType.Currencies,
			Consts.CachePrefix.GameProviderToTgsMap,
			CacheForMinutes,
			id => gameProviderQueries.GetGameProviderCurrenciesByExternalCodeAsync(id));

	Task<Dictionary<string, string>> ICodeMapper.GetMapByInternalCodeAsync(Guid gameProviderId) =>
		GetMappedCodesAsync(
			gameProviderId,
			RedisCacheType.Currencies,
			Consts.CachePrefix.TgsToGameProviderMap,
			CacheForMinutes,
			id => gameProviderQueries.GetGameProviderCurrenciesByInternalCodeAsync(id));

	async Task<string> ICodeMapper.InternalToExternalAsync(string internalCode, Guid gameProviderId)
	{
		var (found, code) = await GetMappedCodeAsync(
			internalCode,
			gameProviderId,
			RedisCacheType.Currencies,
			Consts.CachePrefix.TgsToGameProviderMap,
			"currency",
			CacheForMinutes,
			id => gameProviderQueries.GetGameProviderCurrenciesByInternalCodeAsync(id));

		return !found
			? throw new InvalidOperationException(
				$"Not found code '{internalCode}' for '{Consts.CachePrefix.TgsToGameProviderMap}' "
					+ $"used for 'currency' with filter id '{gameProviderId}' (intended to be cached as {RedisCacheType.Currencies})")
			: code;
	}
}