using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using TGS.Services.Operator;

namespace TGS.Operator.Repositories;

internal class Help2PayRedisRepository(IDatabase database) : IHelp2PayRepository
{
	private static readonly TimeSpan _Expire = TimeSpan.FromHours(1);
	private static readonly string _KeyPrefix = "Help2Pay:DepositHtml";

	private readonly IDatabase _database = database ?? throw new ArgumentNullException(nameof(database));

	public async ValueTask<string?> GetDepositHtmlAsync(Guid identityId)
	{
		var dataKey = $"{_KeyPrefix}:{identityId}";

		return await _database.StringGetAsync(dataKey)
			.ConfigureAwait(false);
	}

	public async ValueTask<bool> StoreDepositHtmlAsync(Guid identityId, string html)
	{
		var dataKey = $"{_KeyPrefix}:{identityId}";

		return await _database.StringSetAsync(dataKey, html, _Expire, when: When.NotExists)
			.ConfigureAwait(false);
	}
}
