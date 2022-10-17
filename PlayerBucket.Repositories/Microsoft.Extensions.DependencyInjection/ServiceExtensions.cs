using PlayerBucket.Repositories;
using TGS.Services.PlayerBucket;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceExtensions
{
    public static IServiceCollection AddPlayerBucketRepositoriesServices(this IServiceCollection services)
    {
		_ = services.AddTransient<IPlayerBucketRepository, PlayerBucketRepository>();

        return services;
    }
}