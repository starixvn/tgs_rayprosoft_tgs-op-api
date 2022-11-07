using System;
using StackExchange.Redis;

namespace TGS.Operator.Repositories;

internal class RedisConnectionFactory
{
	public IDatabase Database { get; }

	public RedisConnectionFactory(
		IConnectionMultiplexer connectionMultiplexer)
	{
		if (connectionMultiplexer is null)
			throw new ArgumentNullException(nameof(connectionMultiplexer));

		Database = connectionMultiplexer.GetDatabase();
	}
}
