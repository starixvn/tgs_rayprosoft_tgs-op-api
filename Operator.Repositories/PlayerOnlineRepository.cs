using System;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Redis;
using TGS.Services.Operator;

namespace TGS.Operator.Repositories;

internal class PlayerOnlineRepository : IPlayerOnlineRepository
{
	private const string PlayerOnlineKey = "playeronline";
	private readonly IDatabase _database;

	public PlayerOnlineRepository(IDatabase database)
	{
		_database = database ?? throw new ArgumentNullException(nameof(database));
	}

	public Task SetAsync(Guid brandId, string playerCode)
		=> _database.StringSetAsync(
			 $"{PlayerOnlineKey}:{brandId}:{playerCode}",
			 JsonSerializer.Serialize(new { Online = DateTimeOffset.UtcNow }),
			 TimeSpan.FromMinutes(1));
}
