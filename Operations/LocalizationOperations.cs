using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using TGS.Core.Infrastructures.Extensions;
using TGS.Core.Infrastructures.Json;
using TGS.Core.Infrastructures.Redis;
using TGS.Core.Models;
using TGS.Core.Models.Enums;
using TGS.Services.Operations.Queries;

namespace TGS.Services.Operations;

public interface ILocalizationOperations
{
    /// <summary>
    ///     OuterKey = Key
    ///     InnerKey = Context
    /// </summary>
    Task<Dictionary<string, Dictionary<string, string>>> GetLocalizationByGameProviderIdAsync(
        Guid gameProviderId,
        string cultureCode = Consts.DefaultCultureCode);

    string Parse(Dictionary<string, Dictionary<string, string>> store, string key, string context,
        string defaultValue);
}

public sealed class LocalizationOperations : ILocalizationOperations
{
    private readonly IRedisCacheManager _cacheManager;
    private readonly IJsonSerializationProvider _json;
    private readonly ILocalizationStringQueries _localizationStringQueries;

    public LocalizationOperations(
        ILocalizationStringQueries localizationStringQueries,
        IRedisCacheManager cacheManager,
        IJsonSerializationProvider json)
    {
        _localizationStringQueries = localizationStringQueries;
        _cacheManager = cacheManager;
        _json = json;
    }

    async Task<Dictionary<string, Dictionary<string, string>>> ILocalizationOperations.
        GetLocalizationByGameProviderIdAsync(Guid gameProviderId, string cultureCode)
    {
        var sets = await _cacheManager.SafeGetAllHashSetAsync(RedisCacheType.Localization, gameProviderId);
        if (sets.Length > 0)
        {
            var value = sets.Where(e => e.Name == cultureCode).Select(e => e.Value).FirstOrDefault();
            return value.HasValue
                ? _json.DeserializeFromString<Dictionary<string, Dictionary<string, string>>>(
                    value)
                : new();
        }

        var r = await _localizationStringQueries.GetLocalizationByGameProviderIdAsync(gameProviderId);
        var localizations = r.GroupBy(lsr => lsr.CultureCode)
            .ToDictionary(
                g => g.Key,
                g => g.ToArray()
                    .GroupBy(ls => ls.Key).ToDictionary(sec => sec.Key,
                        sec => sec.ToDictionary(v => v.Context,
                            v => v.Translation)));

        var fields = localizations.Select(pair =>
            new HashEntry(pair.Key, _json.SerializeToString(pair.Value))).ToArray();

        await _cacheManager.SafeCacheHashSetAsync(RedisCacheType.Localization, gameProviderId, fields,
            TimeSpan.FromDays(1));

        return localizations.TryGetValue(cultureCode, out var store) ? store : null;
    }

    string ILocalizationOperations.Parse(Dictionary<string, Dictionary<string, string>> store,
        string key, string context, string defaultValue)
    {
        if (store == null || !store.TryGetValue(key, out var items))
        {
            return defaultValue;
        }

        return items.TryGetValue(key, out var translation) && translation.IsNotNullOrEmpty()
            ? translation
            : defaultValue;
    }
}