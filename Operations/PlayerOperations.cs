using System;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Core.Infrastructures.Redis;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Exceptions;
using TGS.Core.Models.Services.Caches;
using TGS.Services.Operations.Commands;
using TGS.Services.Operations.Models.Services;
using TGS.Services.Operations.Queries;

namespace TGS.Services.Operations;

public sealed class PlayerOperations : IPlayerOperations
{
	private readonly IRedisCacheManager _cacheManager;

	private readonly IPlayerOperations _interface;
	private readonly IPlayerCommands _playerCommands;
	private readonly Func<IPlayerQueries> _playerQueriesFactory;
	private readonly Func<IWalletQueries> _walletQueriesFactory;

	public PlayerOperations(
		IRedisCacheManager cacheManager,
		Func<IWalletQueries> walletQueriesFactory,
		Func<IPlayerQueries> playerQueriesFactory,
		IPlayerCommands playerCommands)
	{
		_cacheManager = cacheManager;
		_walletQueriesFactory = walletQueriesFactory;
		_playerQueriesFactory = playerQueriesFactory;
		_playerCommands = playerCommands;
		_interface = this;
	}

	public async Task ChangePlayerPasswordAsync(ChangePlayerPasswordData data)
	{
		await _playerCommands.ChangePlayerPasswordAsync(data);
		await _cacheManager.ClearCacheAsync(RedisCacheType.Player, data.PlayerCode);
	}

	public Task<Guid> GetBrandIdByPlayerCodeAsync(string playerCode) =>
		GetCachedPlayerValueAsync(playerCode, CachedPlayerFields.BrandId,
			v => Guid.Parse(v),
			cachedPlayer => cachedPlayer?.BrandId ?? Guid.Empty);

	public async Task<CachedPlayer> GetCachedPlayerAsync(string playerCode)
	{
		var cachedPlayer = await GetCachedPlayerOrNullAsync(playerCode);
		if (cachedPlayer != null)
		{
			return cachedPlayer;
		}

		cachedPlayer = await _playerQueriesFactory().GetCachedPlayerAsync(playerCode);

		if (cachedPlayer == null)
		{
			throw new PlayerNotFoundException(playerCode);
		}

		await CachePlayerAsync(playerCode, cachedPlayer);

		return cachedPlayer;
	}

	public Task<Guid> GetDefaultWalletIdByPlayerCodeAsync(string playerCode) =>
		GetCachedPlayerValueAsync(playerCode, CachedPlayerFields.DefaultWalletId,
			v => Guid.Parse(v),
			cachedPlayer => cachedPlayer?.DefaultWalletId ?? Guid.Empty);

	public Task<decimal> GetPlayerBalanceAsync(Guid walletId, bool dirtyRead) =>
		_walletQueriesFactory().GetBalanceAsync(walletId);

	public Task<Player> GetPlayerByCodeAsync(string playerCode) =>
		_playerQueriesFactory().GetPlayerByCodeAsync(playerCode);

	public Task<string> GetUsernameByPlayerCodeAsync(string playerCode) =>
		GetCachedPlayerValueAsync(playerCode, CachedPlayerFields.Username,
			value => (string)value,
			cachedPlayer => cachedPlayer?.Username);

	#region Private Methods

	public Task SetEmailAsync(string playerCode, string email, DateTimeOffset updatedOn)
		=> _playerCommands.SetEmailAsync(playerCode, email, updatedOn);

	public Task SetIdentityAsync(
		string playerCode,
		string realName,
		string identity,
		bool isAuthenticate,
		DateTimeOffset updatedOn)
		=> _playerCommands.SetIdentityAsync(playerCode, realName, identity, isAuthenticate, updatedOn);

	public Task SetPayPasswordAsync(string playerCode, byte[] newPassword, byte[] newSalt,
		DateTimeOffset updatedOn)
		=> _playerCommands.SetPayPasswordAsync(playerCode, newPassword, newSalt, updatedOn);

	public Task SetQqAsync(string playerCode, string qqId, DateTimeOffset updatedOn)
		=> _playerCommands.SetQqAsync(playerCode, qqId, updatedOn);

	public Task SetWeChatAsync(string playerCode, string weChatId, DateTimeOffset updatedOn)
		=> _playerCommands.SetWeChatAsync(playerCode, weChatId, updatedOn);

	private async Task CachePlayerAsync(string playerCode, CachedPlayer player)
	{
		var fields = new HashEntry[]
		{
			new(CachedPlayerFields.BrandId, player.BrandId.ToString()),
			new(CachedPlayerFields.BrandCode, player.BrandCode),
			new(CachedPlayerFields.Type, (int)player.Type),
			new(CachedPlayerFields.Username, player.Username),
			new(CachedPlayerFields.Status, (int)player.Status),
			new(CachedPlayerFields.DefaultWalletId, player.DefaultWalletId.ToString()),
			new(CachedPlayerFields.DefaultWalletCurrencyCode, player.DefaultWalletCurrencyCode),
			new(CachedPlayerFields.BetLimitId, player.BetLimitId),
			new(CachedPlayerFields.ExternalCurrencyCode, player.ExternalCurrencyCode)
		};

		await _cacheManager.SafeCacheHashSetAsync(RedisCacheType.Player, playerCode, fields, TimeSpan.FromHours(2),
			true);
	}

	private async Task<CachedPlayer> GetCachedPlayerOrNullAsync(string playerCode)
	{
		var values = await _cacheManager.SafeGetHashSetAsync(RedisCacheType.Player, playerCode,
			new RedisValue[]
			{
				CachedPlayerFields.BrandId,
				CachedPlayerFields.Status,
				CachedPlayerFields.Type,
				CachedPlayerFields.Username,
				CachedPlayerFields.DefaultWalletId,
				CachedPlayerFields.BetLimitId,
				CachedPlayerFields.BrandCode,
				CachedPlayerFields.DefaultWalletCurrencyCode,
				CachedPlayerFields.ExternalCurrencyCode
			});

		return values.All(a => !a.IsNull)
			? (new()
			{
				PlayerCode = playerCode,
				BrandId = Guid.Parse(values[0]),
				Status = (PlayerStatus)(int)values[1],
				Type = (PlayerType)(int)values[2],
				Username = values[3],
				DefaultWalletId = Guid.Parse(values[4]),
				BetLimitId = (int)values[5],
				BrandCode = values[6],
				DefaultWalletCurrencyCode = values[7],
				ExternalCurrencyCode = values[8]
			})
			: null;
	}

	private async Task<T> GetCachedPlayerValueAsync<T>(string playerCode, string field, Func<RedisValue, T> cached,
		Func<CachedPlayer, T> noCache)
	{
		var value = await _cacheManager.SafeGetHashSetAsync(RedisCacheType.Player, playerCode, field);
		if (value.HasValue)
		{
			return cached(value);
		}

		var cachedPlayer = await _interface.GetCachedPlayerAsync(playerCode);
		return noCache(cachedPlayer);
	}

	private static class CachedPlayerFields
	{
		public const string BetLimitId = "betLimitId";
		public const string BrandCode = "brandCode";
		public const string BrandId = "brandId";
		public const string DefaultWalletCurrencyCode = "defaultWalletCurrencyCode";
		public const string DefaultWalletId = "defaultWalletId";
		public const string ExternalCurrencyCode = "externalCurrencyCode";
		public const string Status = "status";
		public const string Type = "type";
		public const string Username = "username";
	}

	#endregion Private Methods
}
