using System;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Tgs.CashService.Client;
using Tgs.Payment.Clients;
using TGS.Core.Infrastructures.Mail;
using TGS.Core.Infrastructures.Redis;
using TGS.Services.Operator.GameProviders;
using TGS.Services.Operator.StationLetters;

namespace TGS.Services.Operator.Configuration;

public class OperatorConfiguration
{
    public IServiceCollection Services { get; }

    public OperatorConfiguration(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public OperatorConfiguration AddAgentRepository<TAgentRepository>()
        where TAgentRepository : class, IAgentRepository
    {
        Services.AddTransient<IAgentRepository, TAgentRepository>();

        return this;
    }

    public OperatorConfiguration AddBrandRepository<TBrandRepository>()
        where TBrandRepository : class, IBrandRepository
    {
        Services.AddTransient<IBrandRepository, TBrandRepository>();

        return this;
    }

    public OperatorConfiguration AddGameProviderRepository<TGameProviderRepository>()
        where TGameProviderRepository : class, IGameProviderRepository
    {
        Services.AddTransient<IGameProviderRepository, TGameProviderRepository>();

        return this;
    }

    public OperatorConfiguration AddHelp2PayRedisRepository<THelp2PayRepository>(
        Func<IServiceProvider, IDatabase> redisDatabaseResolver)
        where THelp2PayRepository : class, IHelp2PayRepository
    {
        Services.AddTransient<IHelp2PayRepository>(sp =>
            ActivatorUtilities.CreateInstance<THelp2PayRepository>(sp, redisDatabaseResolver(sp)));

        return this;
    }

    public OperatorConfiguration AddMaintenanceRedisRepository<TMaintenanceRedisRepository>()
        where TMaintenanceRedisRepository : class, IMaintenanceRedisRepository
    {
        Services
            .AddTransient<IMaintenanceRedisRepository, TMaintenanceRedisRepository>();

        return this;
    }

    public OperatorConfiguration AddPaymentServiceClient(Action<IHttpClientBuilder> configureHttpClient)
    {
        Services.AddPaymentServiceClient(configureHttpClient);

        return this;
    }

    public OperatorConfiguration AddPlayerLoginRecordRepository<TPlayerLoginRecordRepository>()
        where TPlayerLoginRecordRepository : class, IPlayerLoginRecordRepository
    {
        _ = Services
            .AddTransient<IPlayerLoginRecordRepository, TPlayerLoginRecordRepository>()
            .Decorate<IPlayerLoginRecordRepository>((innerRepository, sp) =>
            {
                var redisDatabase = sp.GetRequiredService<IRedisConnectionFactory>().GetConnection().GetDatabase();
                return ActivatorUtilities.CreateInstance<RedisPlayerLoginRecordRepository>(
                    sp,
                    innerRepository,
                    redisDatabase);
            })
            .Decorate<IPlayerLoginRecordRepository, MemoryCachedPlayerLoginRecordRepository>();

        return this;
    }

    public OperatorConfiguration AddStationLetterRepository<TStationLetterRepository>()
        where TStationLetterRepository : class, IStationLetterRepository
    {
        Services.AddTransient<IStationLetterRepository, TStationLetterRepository>();

        return this;
    }

    public OperatorConfiguration AddSysRepository<TSysRepository>()
        where TSysRepository : class, ISysRepository
    {
        Services.AddTransient<ISysRepository, TSysRepository>();

        return this;
    }

    public OperatorConfiguration AddVipLevelRepository<TVipLevelRepository>()
        where TVipLevelRepository : class, IVipLevelRepository
    {
        Services.AddTransient<IVipLevelRepository, TVipLevelRepository>();

        return this;
    }

    public OperatorConfiguration AddWalletRepository<TWalletRepository>()
        where TWalletRepository : class, IPlayerRepository
    {
        Services
            .AddTransient<IPlayerRepository, TWalletRepository>();

        return this;
    }

    public OperatorConfiguration ConfigureEmailConfiguration<TDep>(Action<EmailConfiguration, TDep> configure)
        where TDep : class
    {
        Services
            .AddOptions<EmailConfiguration>()
            .Configure(configure);

        return this;
    }

    public OperatorConfiguration ConfigureOperatorOptions<TDep>(Action<OperatorOptions, TDep> configure)
        where TDep : class
    {
        Services
            .AddOptions<OperatorOptions>()
            .Configure(configure);

        return this;
    }

    public OperatorConfiguration ConfigureWallet(Action<CashServiceClientOptions, IServiceProvider> configure)
    {
        Services.AddCashServiceClient(configure);

        return this;
    }
}
