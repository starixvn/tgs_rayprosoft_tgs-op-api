// ReSharper disable CheckNamespace

using System;
using StackExchange.Redis;
using TGS.Operator.Repositories;
using TGS.Services.Operator;
using TGS.Services.Operator.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class OperatorConfigurationExtensions
{
    public static OperatorConfiguration UseOperatorRepositories(this OperatorConfiguration configuration,
        Func<IServiceProvider, IDatabase> redisDatabaseResolver)
    {
        configuration.Services
            .AddSingleton<RedisConnectionFactory>();

        configuration.Services
            .AddSingleton<ISystemClock, DefaultSystemClock>();

        configuration.Services
            .AddTransient<IPlayerOnlineRepository>(sp
                => ActivatorUtilities.CreateInstance<PlayerOnlineRepository>(sp, redisDatabaseResolver(sp)));

        return configuration
            .AddWalletRepository<PlayerRepository>()
            .AddStationLetterRepository<StationLetterRepository>()
            .AddGameProviderRepository<GameProviderRepository>()
            .AddMaintenanceRedisRepository<MaintenanceRedisRepository>()
            .AddSysRepository<SysRepository>()
            .AddBrandRepository<BrandRepository>()
            .AddHelp2PayRedisRepository<Help2PayRedisRepository>(redisDatabaseResolver)
            .AddVipLevelRepository<VipLevelRepository>()
            .AddAgentRepository<AgentRepository>()
            .AddPlayerLoginRecordRepository<PlayerLoginRecordRepository>();
    }
}
