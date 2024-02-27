using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator;

internal class MemoryCachedPlayerLoginRecordRepository(
	IPlayerLoginRecordRepository loginRecordRepository,
	IMemoryCache memoryCache) : IPlayerLoginRecordRepository
{
	private static readonly TimeSpan _Expiry = TimeSpan.FromSeconds(10D);

	private readonly IPlayerLoginRecordRepository _loginRecordRepository = loginRecordRepository ?? throw new ArgumentNullException(nameof(loginRecordRepository));
	private readonly IMemoryCache _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

	public async ValueTask<LoginRecordInfo> AddPlayerLoginRecordAsync(
		Guid brandId,
		string playerCode,
		PlatformType platformType,
		string ipAddress)
	{
		var loginRecord = await _loginRecordRepository
			.AddPlayerLoginRecordAsync(brandId, playerCode, platformType, ipAddress)
			.ConfigureAwait(false);

		var key = $"Tgs:Operator:LoginRecord:{loginRecord.Id}";

		_ = _memoryCache.Set(key, loginRecord, _Expiry);

		return loginRecord;
	}

	public async ValueTask DeauthorizePlayerAsync(Guid loginRecordId)
	{
		var key = $"Tgs:Operator:LoginRecord:{loginRecordId}";

		await _loginRecordRepository
			.DeauthorizePlayerAsync(loginRecordId)
			.ConfigureAwait(false);

		_memoryCache.Remove(key);
	}

	public async ValueTask<LoginRecordInfo?> GetPlayerLoginRecordAsync(Guid loginRecordId)
	{
		var key = $"Tgs:Operator:LoginRecord:{loginRecordId}";

		var loginRecord = await _memoryCache.GetOrCreateAsync(key,
			async entry =>
			{
				var loginRecordInfo = await _loginRecordRepository
					.GetPlayerLoginRecordAsync(loginRecordId)
					.ConfigureAwait(false);

				_ = entry.SetAbsoluteExpiration(_Expiry);

				return loginRecordInfo;
			});

		return loginRecord;
	}
}