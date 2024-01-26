using System;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Redis;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator;

internal class RedisPlayerLoginRecordRepository(
	IPlayerLoginRecordRepository loginRecordRepository,
	IDatabase redisDatabase) : IPlayerLoginRecordRepository
{
	private static readonly TimeSpan _Expiry = TimeSpan.FromHours(1D);

	private static readonly JsonSerializerOptions _SerializerOptions = new()
	{
		WriteIndented = false,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	private readonly IPlayerLoginRecordRepository _loginRecordRepository = loginRecordRepository ?? throw new ArgumentNullException(nameof(loginRecordRepository));
	private readonly IDatabase _redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));

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

		var json = JsonSerializer.Serialize(loginRecord, _SerializerOptions);

		_ = await _redisDatabase.StringSetAsync(
			key,
			json,
			_Expiry,
			when: When.Always).ConfigureAwait(false);

		return loginRecord;
	}

	public async ValueTask DeauthorizePlayerAsync(Guid loginRecordId)
	{
		await _loginRecordRepository.DeauthorizePlayerAsync(loginRecordId).ConfigureAwait(false);

		var key = $"Tgs:Operator:LoginRecord:{loginRecordId}";
		_ = _redisDatabase.KeyDeleteAsync(key).ConfigureAwait(false);
	}

	public async ValueTask<LoginRecordInfo?> GetPlayerLoginRecordAsync(Guid loginRecordId)
	{
		var key = $"Tgs:Operator:LoginRecord:{loginRecordId}";

		var json = await _redisDatabase.StringGetAsync(key).ConfigureAwait(false);

		if (json.HasValue)
		{
			var j = (string?)json;
			return JsonSerializer.Deserialize<LoginRecordInfo>(j!, _SerializerOptions);
		}

		var loginRecord = await _loginRecordRepository.GetPlayerLoginRecordAsync(loginRecordId).ConfigureAwait(false);

		var value = JsonSerializer.Serialize(loginRecord, _SerializerOptions);

		_ = await _redisDatabase.StringSetAsync(
			key,
			value,
			_Expiry,
			when: When.NotExists).ConfigureAwait(false);

		return loginRecord;
	}
}
