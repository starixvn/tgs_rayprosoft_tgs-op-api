using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using TGS.Services.Operator;

namespace TGS.Operator.Repositories;

internal class S88PayRedisRepository(IDatabase database) : IS88PayRepository
{
	private static readonly TimeSpan _Expire = TimeSpan.FromHours(1);
	private static readonly string _KeyPrefix = "S88Pay:Deposit";

	public async ValueTask<string?> GetDepositAsync(Guid id)
	{
		var dataKey = $"{_KeyPrefix}:{id}";

		return await database.StringGetAsync(dataKey)
			.ConfigureAwait(false);
	}

	public async ValueTask<bool> StoreDepositAsync(Guid id, string json)
	{
		var dataKey = $"{_KeyPrefix}:{id}";

		return await database.StringSetAsync(
			key: dataKey,
			value: json,
			expiry: _Expire,
			when: When.NotExists).ConfigureAwait(false);
	}
}