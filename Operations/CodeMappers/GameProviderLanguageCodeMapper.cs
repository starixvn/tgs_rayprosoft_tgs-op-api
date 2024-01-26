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

public interface IGameProviderLanguageCodeMapper : ICodeMapper
{
}

public sealed class GameProviderLanguageCodeMapper(
	IRedisCacheManager cacheManager,
	IJsonSerializationProvider json,
	IGameProviderQueries gameProviderQueries) : BaseCodeMapper(cacheManager, json), IGameProviderLanguageCodeMapper
{
	private const int CacheForMinutes = 30;

	async Task<string> ICodeMapper.ExternalToInternalAsync(string externalCode, Guid gameProviderId)
	{
		var (found, code) = await GetMappedCodeAsync(
			externalCode,
			gameProviderId,
			RedisCacheType.Languages,
			Consts.CachePrefix.GameProviderToTgsMap,
			"language",
			CacheForMinutes,
			id => gameProviderQueries.GetGameProviderLanguagesByExternalCodeAsync(id));

		return !found
			? throw new InvalidOperationException(
				$"Not found code '{externalCode}' for '{Consts.CachePrefix.GameProviderToTgsMap}' "
					+ $"used for 'language' with filter id '{gameProviderId}' (intended to be cached as {RedisCacheType.Languages})")
			: code;
	}

	Task<Dictionary<string, string>> ICodeMapper.GetMapByExternalCodeAsync(Guid gameProviderId) =>
		GetMappedCodesAsync(
			gameProviderId,
			RedisCacheType.Languages,
			Consts.CachePrefix.GameProviderToTgsMap,
			CacheForMinutes,
			id => gameProviderQueries.GetGameProviderLanguagesByExternalCodeAsync(id));

	Task<Dictionary<string, string>> ICodeMapper.GetMapByInternalCodeAsync(Guid gameProviderId) =>
		GetMappedCodesAsync(
			gameProviderId,
			RedisCacheType.Languages,
			Consts.CachePrefix.TgsToGameProviderMap,
			CacheForMinutes,
			id => gameProviderQueries.GetGameProviderLanguagesByInternalCodeAsync(id));

	async Task<string> ICodeMapper.InternalToExternalAsync(string internalCode, Guid gameProviderId)
	{
		var (found, code) = await GetMappedCodeAsync(
			internalCode,
			gameProviderId,
			RedisCacheType.Languages,
			Consts.CachePrefix.TgsToGameProviderMap,
			"language",
			CacheForMinutes,
			id => gameProviderQueries.GetGameProviderLanguagesByInternalCodeAsync(id));

		return !found
			? throw new InvalidOperationException(
				$"Not found code '{internalCode}' for '{Consts.CachePrefix.TgsToGameProviderMap}' "
					+ $"used for 'language' with filter id {gameProviderId} (intended to be cached as {RedisCacheType.Languages})")
			: code;
	}
}
