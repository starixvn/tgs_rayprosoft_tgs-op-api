using System;
using TGS.Apis.OperatorApi;
using TGS.Core.Infrastructures.Redis;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRedis(
        this IServiceCollection service,
        Action<RedisOptions, IServiceProvider> configure)
    {
        service
            .AddOptions<RedisOptions>()
            .Configure(configure)
            .Services
            .AddRedisServices()
            .RegisterRedisConnectionFactory<RedisConnectionFactory>();

        service.AddSingleton(sp =>
        {
            var connectionFactory = sp.GetRequiredService<IRedisConnectionFactory>();
            return connectionFactory.GetConnection();
        });

        return service;
    }
}
