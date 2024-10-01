using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
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

public sealed class PlayerOperations(
	IRedisCacheManager cacheManager,
	Func<IWalletQueries> walletQueriesFactory,
	Func<IPlayerQueries> playerQueriesFactory,
	IPlayerCommands playerCommands,
	IConfiguration configuration) : IPlayerOperations
{
	public async Task ChangePlayerPasswordAsync(ChangePlayerPasswordData data)
	{
		await playerCommands.ChangePlayerPasswordAsync(data);
		await cacheManager.ClearCacheAsync(RedisCacheType.Player, data.PlayerCode);
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

		cachedPlayer = await playerQueriesFactory().GetCachedPlayerAsync(playerCode);

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
		walletQueriesFactory().GetBalanceAsync(walletId);

	public Task<Player> GetPlayerByCodeAsync(string playerCode) =>
		playerQueriesFactory().GetPlayerByCodeAsync(playerCode);

	public Task<string> GetUsernameByPlayerCodeAsync(string playerCode) =>
		GetCachedPlayerValueAsync(playerCode, CachedPlayerFields.Username,
			value => (string)value,
			cachedPlayer => cachedPlayer?.Username);

	#region Private Methods

	public Task SetEmailAsync(string playerCode, string email, DateTimeOffset updatedOn)
		=> playerCommands.SetEmailAsync(playerCode, email, updatedOn);

	public Task SetPhoneAsync(string playerCode, string areaCode, string phone, DateTimeOffset updatedOn) 
		=> playerCommands.SetPhoneAsync(playerCode, areaCode, phone, updatedOn);

	public Task SetIdentityAsync(
		string playerCode,
		string realName,
		string identity,
		bool isAuthenticate,
		DateTimeOffset updatedOn)
		=> playerCommands.SetIdentityAsync(playerCode, realName, identity, isAuthenticate, updatedOn);

	public Task SetPayPasswordAsync(string playerCode, byte[] newPassword, byte[] newSalt,
		DateTimeOffset updatedOn)
		=> playerCommands.SetPayPasswordAsync(playerCode, newPassword, newSalt, updatedOn);

	public Task SetQqAsync(string playerCode, string qqId, DateTimeOffset updatedOn)
		=> playerCommands.SetQqAsync(playerCode, qqId, updatedOn);

	public Task SetWeChatAsync(string playerCode, string weChatId, DateTimeOffset updatedOn)
		=> playerCommands.SetWeChatAsync(playerCode, weChatId, updatedOn);

	public string DecryptPassword(string encryptedPassword) {
		string privateKeyPem = configuration.GetSection("RSAKeys:PrivateKey").Value;

		using RSA rsa = RSA.Create();
		rsa.ImportFromPem(privateKeyPem.ToCharArray());

		byte[] encryptedBytes = Convert.FromBase64String(encryptedPassword);
		byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.OaepSHA256);

		return Encoding.UTF8.GetString(decryptedBytes);
	}

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

		await cacheManager.SafeCacheHashSetAsync(RedisCacheType.Player, playerCode, fields, TimeSpan.FromHours(2),
			true);
	}

	private async Task<CachedPlayer> GetCachedPlayerOrNullAsync(string playerCode)
	{
		var values = await cacheManager.SafeGetHashSetAsync(
			RedisCacheType.Player,
			playerCode,
			[
				CachedPlayerFields.BrandId,
				CachedPlayerFields.Status,
				CachedPlayerFields.Type,
				CachedPlayerFields.Username,
				CachedPlayerFields.DefaultWalletId,
				CachedPlayerFields.BetLimitId,
				CachedPlayerFields.BrandCode,
				CachedPlayerFields.DefaultWalletCurrencyCode,
				CachedPlayerFields.ExternalCurrencyCode
			]);

		return values.All(a => !a.IsNull)
			? new()
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
			}
			: null;
	}

	private async Task<T> GetCachedPlayerValueAsync<T>(string playerCode, string field, Func<RedisValue, T> cached,
		Func<CachedPlayer, T> noCache)
	{
		var value = await cacheManager.SafeGetHashSetAsync(RedisCacheType.Player, playerCode, field);
		if (value.HasValue)
		{
			return cached(value);
		}

		var cachedPlayer = await GetCachedPlayerAsync(playerCode);
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