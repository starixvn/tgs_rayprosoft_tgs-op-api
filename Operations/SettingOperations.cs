using System;
using System.Threading.Tasks;
using TGS.Core.Infrastructures.Extensions;
using TGS.Core.Infrastructures.Json;
using TGS.Core.Infrastructures.Redis;
using TGS.Core.Models.Enums;
using TGS.Services.Operations.Queries;

namespace TGS.Services.Operations;

public interface ISettingOperations
{
    Task<string> GetValueByKeyAsync(string key);

    Task<int> GetIntegerValueByKeyAsync(string key, int defaultValue = 0);

    Task<decimal> GetDecimalValueByKeyAsync(string key, decimal defaultValue = 0m);

    Task<bool> GetBooleanValueByKeyAsync(string key, bool defaultValue = false);

    Task<T> GetValueFromJsonByKeyAsync<T>(string key);
}

public sealed class SettingOperations : ISettingOperations
{
    private readonly IRedisCacheManager _cacheManager;
    private readonly IJsonSerializationProvider _json;
    private readonly ISettingQueries _settingQueries;

    public SettingOperations(
        ISettingQueries settingQueries,
        IRedisCacheManager cacheManager,
        IJsonSerializationProvider json)
    {
        _settingQueries = settingQueries;
        _cacheManager = cacheManager;
        _json = json;
    }

    async Task<bool> ISettingOperations.GetBooleanValueByKeyAsync(string key, bool defaultValue)
    {
        var value = await GetByKeyAsync(key);
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    async Task<decimal> ISettingOperations.GetDecimalValueByKeyAsync(string key, decimal defaultValue)
    {
        var value = await GetByKeyAsync(key);
        return decimal.TryParse(value, out var result) ? result : defaultValue;
    }

    async Task<int> ISettingOperations.GetIntegerValueByKeyAsync(string key, int defaultValue)
    {
        var value = await GetByKeyAsync(key);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    Task<string> ISettingOperations.GetValueByKeyAsync(string key) =>
        GetByKeyAsync(key);

    async Task<T> ISettingOperations.GetValueFromJsonByKeyAsync<T>(string key)
    {
        var value = await GetByKeyAsync(key);

        if (value.IsNullOrEmpty())
        {
            throw new ArgumentNullException($"Setting value for {key} not found");
        }

        var result = _json.DeserializeFromString<T>(value);
        return result != null
            ? result
            : throw new InvalidOperationException(
                $"Setting value '{value}' for {key} can't be deserialized as {typeof(T).Name}");
    }

    private Task<string> GetByKeyAsync(string key) =>
        _cacheManager.SafeGetAndCacheAsync(RedisCacheType.Setting, key, TimeSpan.FromDays(1),
            () => _settingQueries.GetSettingValueByKeyAsync(key));
}