using System;
using StackExchange.Redis;

namespace TGS.Operator.Repositories;

internal class RedisConnectionFactory
{
	public IDatabase Database { get; }

	public RedisConnectionFactory(
		IConnectionMultiplexer connectionMultiplexer)
	{
		ArgumentNullException.ThrowIfNull(connectionMultiplexer);

		Database = connectionMultiplexer.GetDatabase();
	}
}