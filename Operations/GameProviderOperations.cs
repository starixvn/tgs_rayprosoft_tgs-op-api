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
using TGS.Core.Models.Services.Caches;
using TGS.Core.Models.Services.Data;
using TGS.Services.Operations.Queries;

namespace TGS.Services.Operations;

public interface IGameProviderOperations
{
    Task<Guid> GetGameProviderIdByCodeAsync(string gameProviderCode);

    Task<CachedGameProvider> GetCachedGameProviderAsync(Guid gameProviderId);

    Task<CachedGameProviderConfiguration> GetCachedGameProviderConfigurationAsync(Guid gameProviderId,
        Guid gameProviderConfigurationId);

    Task<Status?> GetGameProviderStatusAsync(Guid gameProviderId);

    Task<Status?> GetGameProviderConfigurationStatusAsync(Guid gameProviderId, Guid gameProviderConfigurationId);

    Task<bool> GetGameProviderSupportTestPlayerAsync(Guid gameProviderId);

    Task<string[]> GetGameProviderLanguagesAsync(Guid gameProviderId);

    Task<bool> LanguageSupportedAsync(Guid gameProviderId, string cultureCode);

    Task<bool> CurrencySupportedAsync(Guid gameProviderId, string currencyCode);

    Task<string[]> GetGameProviderCurrenciesAsync(Guid gameProviderId);

    Task<CachedGame[]> GetCachedGamesAsync(Guid gameProviderId);

    Task<CachedGame[]> GetCachedGamesAsync(Guid gameProviderId, string externalGameId);

    Task<CachedGame[]> GetCachedGamesAsync(Guid gameProviderId, GamePlatformType[] platformTypes,
        GameType gameType);

    Task<CachedGame[]> GetCachedGamesAsync(Guid gameProviderId, GamePlatformType[] platformTypes);

    Task<CachedGame> GetCachedGameAsync(Guid gameProviderId, int gameId);

    Task<GameStatus?> GetGameStatusAsync(Guid gameProviderId, int gameId);

    Task<GameType?> GetGameTypeAsync(Guid gameProviderId, int gameId);

    Task<GamePlatformType?> GetGamePlatformTypeAsync(Guid gameProviderId, int gameId);

    Task<CachedSubGame[]> GetSubGamesAsync(Guid gameProviderId, GamePlatformType[] platformTypes);

    Task<bool> GetAllowKickPlayerAsync(Guid gameProviderId);

    Task<bool> GetAutoCloseRoundAsync(Guid gameProviderId);

    Task<IdAndCodeAndConfigurationId> GetGameProviderIdsByCredentialsAsync(string clientId, string secretKey);

    Task<Dictionary<Guid, string>> GetGameProvidersLocalizationByGameProviderIdsAsync(Guid[] gameProviderIds);
}

public sealed class GameProviderOperations : IGameProviderOperations
{
    private readonly IRedisCacheManager _cacheManager;
    private readonly IGameProviderQueries _gameProviderQueries;
    private readonly IGameProviderOperations _interface;
    private readonly IJsonSerializationProvider _json;

    public GameProviderOperations(
        IRedisCacheManager cacheManager,
        IGameProviderQueries gameProviderQueries,
        IJsonSerializationProvider json)
    {
        _cacheManager = cacheManager;
        _gameProviderQueries = gameProviderQueries;
        _json = json;
        _interface = this;
    }

    async Task<CachedGameProvider> IGameProviderOperations.GetCachedGameProviderAsync(Guid gameProviderId)
    {
        var cachedGameProvider = await GetCachedGameProviderOrNullAsync(gameProviderId);
        if (cachedGameProvider != null)
        {
            return cachedGameProvider;
        }

        var r = await _gameProviderQueries.GetCachedGameProviderAsync(gameProviderId);
        cachedGameProvider = new()
        {
            Id = gameProviderId,
            Code = r.Code,
            Name = r.Name,
            Description = r.Description,
            Status = r.Status,
            AutoCloseRound = r.AutoCloseRound,
            SupportTestPlayer = r.SupportTestPlayer,
            PlaceBetTimeout = r.PlaceBetTimeout,
            SettleBetTimeout = r.SettleBetTimeout,
            AllowKickPlayer = r.AllowKickPlayer,
            IsTransferWallet = r.IsTransferWallet
        };

        await CacheGameProviderAsync(gameProviderId, cachedGameProvider);

        return cachedGameProvider;
    }

    async Task<CachedGameProviderConfiguration> IGameProviderOperations.GetCachedGameProviderConfigurationAsync(
        Guid gameProviderId, Guid gameProviderConfigurationId)
    {
        var gpc = await GetCachedGameProviderConfigurationOrNullAsync(gameProviderId, gameProviderConfigurationId);

        if (gpc != null)
        {
            return gpc;
        }

        var r = await _gameProviderQueries
            .GetCachedGameProviderConfigurationAsync(gameProviderConfigurationId);
        if (r == null)
        {
            throw new InvalidOperationException(
                $"GameProviderConfiguration doesn't exist with gameProviderId: {gameProviderId} and gameProviderConfigurationId: {gameProviderConfigurationId}");
        }

        gpc = new()
        {
            Status = r.Status,
            GameProviderId = gameProviderId,
            SecretKey = r.SecretKey,
            DesktopEndpoint = r.DesktopEndpoint,
            MobileEndpoint = r.MobileEndpoint,
            ConfigurationData = r.ConfigurationData,
            IsDefault = r.IsDefault
        };

        await CacheGameProviderConfigurationAsync(gameProviderId, gameProviderConfigurationId, gpc);

        return gpc;
    }

    async Task<Status?> IGameProviderOperations.GetGameProviderConfigurationStatusAsync(Guid gameProviderId,
        Guid gameProviderConfigurationId)
    {
        var value = await _cacheManager.SafeGetHashSetAsync(RedisCacheType.GameProviderConfiguration,
            gameProviderId, gameProviderConfigurationId.ToString());
        var gpc = value.HasValue
            ? _json.DeserializeFromString<CachedGameProviderConfiguration>(value)
            : await _interface.GetCachedGameProviderConfigurationAsync(gameProviderId, gameProviderConfigurationId);

        return gpc.Status;
    }

    Task<Guid> IGameProviderOperations.GetGameProviderIdByCodeAsync(string gameProviderCode) =>
        _cacheManager.SafeGetAndCacheAsync(RedisCacheType.GameProviderIdByCode, gameProviderCode,
            TimeSpan.FromHours(2),
            () => _gameProviderQueries.GetGameProviderIdByCodeAsync(gameProviderCode));

    Task<Status?> IGameProviderOperations.GetGameProviderStatusAsync(Guid gameProviderId) =>
        GetCachedGameProviderValueAsync(gameProviderId, CachedGameProviderFields.Status,
            v => (Status)(int)v, gp => gp?.Status);

    Task<bool> IGameProviderOperations.GetGameProviderSupportTestPlayerAsync(Guid gameProviderId) =>
        GetCachedGameProviderValueAsync(gameProviderId, CachedGameProviderFields.SupportTestPlayer,
            v => (bool)v, gp => gp?.SupportTestPlayer ?? false);

    Task<string[]> IGameProviderOperations.GetGameProviderLanguagesAsync(Guid gameProviderId) =>
        _cacheManager.SafeGetAndCacheAsync(RedisCacheType.GameProviderLanguages,
            gameProviderId, TimeSpan.FromHours(2),
            async () =>
            {
                var dict = await _gameProviderQueries
                    .GetGameProviderLanguagesByInternalCodeAsync(gameProviderId);
                return dict.Keys.ToArray();
            });

    async Task<bool> IGameProviderOperations.LanguageSupportedAsync(Guid gameProviderId, string cultureCode)
    {
        var languages = await _interface.GetGameProviderLanguagesAsync(gameProviderId);
        return languages.IgnoreCaseContains(cultureCode);
    }

    async Task<bool> IGameProviderOperations.CurrencySupportedAsync(Guid gameProviderId, string currencyCode)
    {
        var currencies = await _cacheManager.SafeGetAndCacheAsync(RedisCacheType.GameProviderCurrencies,
            gameProviderId, TimeSpan.FromHours(2),
            async () =>
            {
                var dict = await _gameProviderQueries
                    .GetGameProviderCurrenciesByInternalCodeAsync(gameProviderId);
                return dict.Keys.ToArray();
            });

        return currencies.IgnoreCaseContains(currencyCode);
    }

    async Task<string[]> IGameProviderOperations.GetGameProviderCurrenciesAsync(Guid gameProviderId) =>
        await _cacheManager.SafeGetAndCacheAsync(RedisCacheType.GameProviderCurrencies,
            gameProviderId, TimeSpan.FromHours(2),
            async () =>
            {
                var dict = await _gameProviderQueries
                    .GetGameProviderCurrenciesByInternalCodeAsync(gameProviderId);
                return dict.Keys.ToArray();
            });

    async Task<CachedGame[]> IGameProviderOperations.GetCachedGamesAsync(Guid gameProviderId)
    {
        var sets = await _cacheManager.SafeGetAllHashSetAsync(RedisCacheType.Games, gameProviderId);

        return sets.Length > 0
            ? sets.Select(s => _json.DeserializeFromString<CachedGame>(s.Value)).ToArray()
            : await GetAndCacheGamesAsync(gameProviderId);
    }

    async Task<CachedGame[]> IGameProviderOperations.GetCachedGamesAsync(Guid gameProviderId, string externalGameId)
    {
        var cachedGames = await _interface.GetCachedGamesAsync(gameProviderId);
        return cachedGames.Where(g => g.ExternalId.IgnoreCaseEquals(externalGameId)).ToArray();
    }

    async Task<CachedGame[]> IGameProviderOperations.GetCachedGamesAsync(Guid gameProviderId,
        GamePlatformType[] platformTypes, GameType gameType)
    {
        var types = platformTypes?.Length > 0 ? platformTypes : Consts.AllGamePlatformTypes;
        var cachedGames = await _interface.GetCachedGamesAsync(gameProviderId);
        return cachedGames.Where(g => types.Contains(g.PlatformType) && g.Type == gameType).ToArray();
    }

    async Task<CachedGame[]> IGameProviderOperations.GetCachedGamesAsync(Guid gameProviderId,
        GamePlatformType[] platformTypes)
    {
        var types = platformTypes?.Length > 0 ? platformTypes : Consts.AllGamePlatformTypes;
        var cachedGames = await _interface.GetCachedGamesAsync(gameProviderId);
        return cachedGames.Where(g => types.Contains(g.PlatformType)).ToArray();
    }

    async Task<CachedGame> IGameProviderOperations.GetCachedGameAsync(Guid gameProviderId, int gameId)
    {
        var value = await _cacheManager.SafeGetHashSetAsync(RedisCacheType.Games, gameProviderId,
            gameId.ToString());
        if (value.HasValue)
        {
            return _json.DeserializeFromString<CachedGame>(value);
        }

        var games = await GetAndCacheGamesAsync(gameProviderId);
        return games.FirstOrDefault(g => g.Id == gameId);
    }

    async Task<GameStatus?> IGameProviderOperations.GetGameStatusAsync(Guid gameProviderId, int gameId)
    {
        var game = await _interface.GetCachedGameAsync(gameProviderId, gameId);
        return game?.Status;
    }

    async Task<GameType?> IGameProviderOperations.GetGameTypeAsync(Guid gameProviderId, int gameId)
    {
        var game = await _interface.GetCachedGameAsync(gameProviderId, gameId);
        return game?.Type;
    }

    async Task<GamePlatformType?> IGameProviderOperations.GetGamePlatformTypeAsync(Guid gameProviderId, int gameId)
    {
        var game = await _interface.GetCachedGameAsync(gameProviderId, gameId);
        return game?.PlatformType;
    }

    Task<bool> IGameProviderOperations.GetAllowKickPlayerAsync(Guid gameProviderId) =>
        GetCachedGameProviderValueAsync(gameProviderId, CachedGameProviderFields.AllowKickPlayer,
            v => (bool)v, gp => gp?.AllowKickPlayer ?? false);

    Task<bool> IGameProviderOperations.GetAutoCloseRoundAsync(Guid gameProviderId) =>
        GetCachedGameProviderValueAsync(gameProviderId, CachedGameProviderFields.AutoCloseRound,
            v => (bool)v, gp => gp?.AutoCloseRound ?? true);

    async Task<CachedSubGame[]> IGameProviderOperations.GetSubGamesAsync(Guid gameProviderId,
        GamePlatformType[] platformTypes)
    {
        var types = platformTypes?.Length > 0 ? platformTypes : Consts.AllGamePlatformTypes;
        var games = await _cacheManager.SafeGetAndCacheAsync(RedisCacheType.SubGames, gameProviderId,
            TimeSpan.FromHours(2), () => _gameProviderQueries.GetSubGamesAsync(gameProviderId));

        return games.Where(g => types.Contains(g.PlatformType)).ToArray();
    }

    #region Private Methods

    private async Task<CachedGameProvider> GetCachedGameProviderOrNullAsync(Guid gameProviderId)
    {
        var values = await _cacheManager.SafeGetHashSetAsync(RedisCacheType.GameProvider, gameProviderId,
            new RedisValue[]
            {
                CachedGameProviderFields.GameProviderId, CachedGameProviderFields.GameProviderCode,
                CachedGameProviderFields.GameProviderName, CachedGameProviderFields.Status,
                CachedGameProviderFields.Description, CachedGameProviderFields.AutoCloseRound,
                CachedGameProviderFields.SupportTestPlayer, CachedGameProviderFields.PlaceBetTimeout,
                CachedGameProviderFields.SettleBetTimeout, CachedGameProviderFields.AllowKickPlayer,
                CachedGameProviderFields.IsTransferWallet
            });

        if (values.All(a => !a.IsNull))
        {
            return new()
            {
                Id = Guid.Parse(values[0]),
                Code = values[1],
                Name = values[2],
                Status = (Status)(int)values[3],
                Description = values[4],
                AutoCloseRound = (bool)values[5],
                SupportTestPlayer = (bool)values[6],
                PlaceBetTimeout = (int)values[7],
                SettleBetTimeout = (int)values[8],
                AllowKickPlayer = (bool)values[9],
                IsTransferWallet = (bool)values[10]
            };
        }

        return null;
    }

    private async Task CacheGameProviderAsync(Guid gameProviderId, CachedGameProvider gameProvider)
    {
        var fields = new HashEntry[]
        {
            new(CachedGameProviderFields.GameProviderId, gameProvider.Id.ToString()),
            new(CachedGameProviderFields.GameProviderCode, gameProvider.Code),
            new(CachedGameProviderFields.GameProviderName, gameProvider.Name),
            new(CachedGameProviderFields.Status, (int)gameProvider.Status),
            new(CachedGameProviderFields.Description, gameProvider.Description),
            new(CachedGameProviderFields.AutoCloseRound, gameProvider.AutoCloseRound),
            new(CachedGameProviderFields.SupportTestPlayer, gameProvider.SupportTestPlayer),
            new(CachedGameProviderFields.PlaceBetTimeout, gameProvider.PlaceBetTimeout),
            new(CachedGameProviderFields.SettleBetTimeout, gameProvider.SettleBetTimeout),
            new(CachedGameProviderFields.AllowKickPlayer, gameProvider.AllowKickPlayer),
            new(CachedGameProviderFields.IsTransferWallet, gameProvider.IsTransferWallet)
        };

        await _cacheManager.SafeCacheHashSetAsync(RedisCacheType.GameProvider, gameProviderId, fields,
            TimeSpan.FromHours(2), true);
    }

    private async Task<T> GetCachedGameProviderValueAsync<T>(Guid gameProviderId, string field,
        Func<RedisValue, T> cached, Func<CachedGameProvider, T> noCache)
    {
        var value = await _cacheManager.SafeGetHashSetAsync(RedisCacheType.GameProvider, gameProviderId, field);
        if (value.HasValue)
        {
            return cached(value);
        }

        var gameProvider = await _interface.GetCachedGameProviderAsync(gameProviderId);
        return noCache(gameProvider);
    }

    private async Task<CachedGameProviderConfiguration> GetCachedGameProviderConfigurationOrNullAsync(
        Guid gameProviderId, Guid gameProviderConfigurationId)
    {
        var data = await _cacheManager.SafeGetHashSetAsync(RedisCacheType.GameProviderConfiguration, gameProviderId,
            gameProviderConfigurationId.ToString());

        return data.HasValue ? _json.DeserializeFromString<CachedGameProviderConfiguration>(data) : null;
    }

    private async Task CacheGameProviderConfigurationAsync(Guid gameProviderId, Guid gameProviderConfigurationId,
        CachedGameProviderConfiguration gpc)
    {
        var fields = new HashEntry[] { new(gameProviderConfigurationId.ToString(), _json.SerializeToString(gpc)) };

        await _cacheManager.SafeCacheHashSetAsync(RedisCacheType.GameProviderConfiguration, gameProviderId, fields,
            TimeSpan.FromHours(2), true);
    }

    private async Task<CachedGame[]> GetAndCacheGamesAsync(Guid gameProviderId)
    {
        var games = await _gameProviderQueries.GetCachedGamesAsync(gameProviderId);

        var fields = games.Select(g => new HashEntry(g.Id.ToString(), _json.SerializeToString(g)))
            .ToArray();

        await _cacheManager.SafeCacheHashSetAsync(RedisCacheType.Games, gameProviderId, fields,
            TimeSpan.FromHours(2));
        return games;
    }

    Task<IdAndCodeAndConfigurationId> IGameProviderOperations.GetGameProviderIdsByCredentialsAsync(string clientId,
        string secretKey) =>
        _cacheManager.SafeGetAndCacheAsync(
            RedisCacheType.GameProviderIdAndConfigurationIdByCredentials,
            $"{clientId}{secretKey}", TimeSpan.FromDays(1),
            () => _gameProviderQueries.GetGameProviderIdsByCredentialsAsync(clientId, secretKey));

    public Task<Dictionary<Guid, string>> GetGameProvidersLocalizationByGameProviderIdsAsync(Guid[] gameProviderIds)
    {
        return _gameProviderQueries.GetGameProvidersLocalizationByGameProviderIdsAsync(gameProviderIds);
    }

    private static class CachedGameProviderFields
    {
        public const string GameProviderId = "gameProviderId";
        public const string GameProviderCode = "gameProviderCode";
        public const string GameProviderName = "gameProviderName";
        public const string Status = "status";
        public const string Description = "description";
        public const string AutoCloseRound = "autoClosedRound";
        public const string SupportTestPlayer = "supportTestPlayer";
        public const string PlaceBetTimeout = "placeBetTimeout";
        public const string SettleBetTimeout = "settleBetTimeout";
        public const string AllowKickPlayer = "allowKickPlayer";
        public const string IsTransferWallet = "isTransferWallet";
    }

    #endregion Private Methods
}