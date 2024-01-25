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

public interface IBrandCurrencyCodeMapper : ICodeMapper
{
}

public sealed class BrandCurrencyCodeMapper(
	IRedisCacheManager cacheManager,
	IJsonSerializationProvider json,
	IBrandQueries brandQueries) : BaseCodeMapper(cacheManager, json), IBrandCurrencyCodeMapper
{
	private const int CacheForMinutes = 30;

	async Task<string> ICodeMapper.ExternalToInternalAsync(string externalCode, Guid brandId)
	{
		var (found, code) = await GetMappedCodeAsync(
			externalCode,
			brandId,
			RedisCacheType.Currencies,
			Consts.CachePrefix.BrandToTgsMap,
			"currency",
			CacheForMinutes,
			id => brandQueries.GetBrandCurrenciesByExternalCodeAsync(id));

		return !found
			? throw new InvalidOperationException(
				$"Not found code '{externalCode}' for '{Consts.CachePrefix.BrandToTgsMap}' "
					+ $"used for 'currency' with filter id '{brandId}' (intended to be cached as {RedisCacheType.Currencies})")
			: code;
	}

	Task<Dictionary<string, string>> ICodeMapper.GetMapByExternalCodeAsync(Guid brandId) =>
		GetMappedCodesAsync(brandId, RedisCacheType.Currencies,
			Consts.CachePrefix.BrandToTgsMap, CacheForMinutes,
			id => brandQueries.GetBrandCurrenciesByExternalCodeAsync(id));

	Task<Dictionary<string, string>> ICodeMapper.GetMapByInternalCodeAsync(Guid brandId) =>
		GetMappedCodesAsync(brandId, RedisCacheType.Currencies,
			Consts.CachePrefix.TgsToBrandMap, CacheForMinutes,
			id => brandQueries.GetBrandCurrenciesByInternalCodeAsync(id));

	async Task<string> ICodeMapper.InternalToExternalAsync(string internalCode, Guid brandId)
	{
		var (found, code) = await GetMappedCodeAsync(
			internalCode,
			brandId,
			RedisCacheType.Currencies,
			Consts.CachePrefix.TgsToBrandMap,
			"currency",
			CacheForMinutes,
			id => brandQueries.GetBrandCurrenciesByInternalCodeAsync(id));

		return !found
			? throw new InvalidOperationException(
				$"Not found code '{internalCode}' for '{Consts.CachePrefix.TgsToBrandMap}' "
					+ $"used for 'currency' with filter id '{brandId}' (intended to be cached as {RedisCacheType.Currencies})")
			: code;
	}
}
