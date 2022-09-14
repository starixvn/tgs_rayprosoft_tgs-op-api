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

public interface IBrandLanguageCodeMapper : ICodeMapper
{
}

public sealed class BrandLanguageCodeMapper : BaseCodeMapper, IBrandLanguageCodeMapper
{
    private const int CacheForMinutes = 30;
    private readonly IBrandQueries _brandQueries;

    public BrandLanguageCodeMapper(
        IRedisCacheManager cacheManager,
        IJsonSerializationProvider json,
        IBrandQueries brandQueries)
        : base(cacheManager, json)
    {
        _brandQueries = brandQueries;
    }

    async Task<string> ICodeMapper.ExternalToInternalAsync(string externalCode, Guid brandId)
    {
        var (found, code) = await GetMappedCodeAsync(
            externalCode,
            brandId,
            RedisCacheType.Languages,
            Consts.CachePrefix.BrandToTgsMap,
            "language",
            CacheForMinutes,
            id => _brandQueries.GetBrandLanguagesByExternalCodeAsync(id));

        if (!found)
        {
            throw new InvalidOperationException(
                $"Not found code '{externalCode}' for '{Consts.CachePrefix.BrandToTgsMap}' used for 'language' with filter id '{brandId}' (intended to be cached as {RedisCacheType.Languages})");
        }

        return code;
    }

    async Task<string> ICodeMapper.InternalToExternalAsync(string internalCode, Guid brandId)
    {
        var (found, code) = await GetMappedCodeAsync(
            internalCode,
            brandId,
            RedisCacheType.Languages,
            Consts.CachePrefix.TgsToBrandMap,
            "language",
            CacheForMinutes,
            id => _brandQueries.GetBrandLanguagesByInternalCodeAsync(id));

        if (!found)
        {
            throw new InvalidOperationException(
                $"Not found code '{internalCode}' for '{Consts.CachePrefix.TgsToBrandMap}' used for 'language' with filter id '{brandId}' (intended to be cached as {RedisCacheType.Languages})");
        }

        return code;
    }

    Task<Dictionary<string, string>> ICodeMapper.GetMapByExternalCodeAsync(Guid brandId) =>
        GetMappedCodesAsync(brandId, RedisCacheType.Languages,
            Consts.CachePrefix.BrandToTgsMap, CacheForMinutes,
            id => _brandQueries.GetBrandLanguagesByExternalCodeAsync(id));

    Task<Dictionary<string, string>> ICodeMapper.GetMapByInternalCodeAsync(Guid brandId) =>
        GetMappedCodesAsync(brandId, RedisCacheType.Languages,
            Consts.CachePrefix.TgsToBrandMap, CacheForMinutes,
            id => _brandQueries.GetBrandLanguagesByInternalCodeAsync(id));
}