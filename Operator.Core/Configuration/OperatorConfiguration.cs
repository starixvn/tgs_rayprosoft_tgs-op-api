using System;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Tgs.CashService.Client;
using TGS.Core.Infrastructures.Mail;
using TGS.Core.Infrastructures.Redis;
using TGS.Services.Operator.ApplicationServices.MemoryCache;
using TGS.Services.Operator.GameProviders;
using TGS.Services.Operator.StationLetters;

namespace TGS.Services.Operator.Configuration;

public class OperatorConfiguration(IServiceCollection services)
{
	public IServiceCollection Services { get; } = services ?? throw new ArgumentNullException(nameof(services));

	public OperatorConfiguration AddAgentRepository<TAgentRepository>()
		where TAgentRepository : class, IAgentRepository
	{
		_ = Services.AddTransient<IAgentRepository, TAgentRepository>();

		return this;
	}

	public OperatorConfiguration AddBrandRepository<TBrandRepository>()
		where TBrandRepository : class, IBrandRepository
	{
		_ = Services.AddTransient<IBrandRepository, TBrandRepository>()
			  .Decorate<IBrandRepository, MemoryCachedBrandRepository>();

		return this;
	}

	public OperatorConfiguration AddGameProviderRepository<TGameProviderRepository>()
		where TGameProviderRepository : class, IGameProviderRepository
	{
		_ = Services.AddTransient<IGameProviderRepository, TGameProviderRepository>();

		return this;
	}

	public OperatorConfiguration AddCategoryRepository<TCategoryRepository>()
		where TCategoryRepository : class, IGameTypeRepository
	{
		_ = Services.AddTransient<IGameTypeRepository, TCategoryRepository>();

		return this;
	}

	public OperatorConfiguration AddHelp2PayRedisRepository<THelp2PayRepository>(
		Func<IServiceProvider, IDatabase> redisDatabaseResolver)
		where THelp2PayRepository : class, IHelp2PayRepository
	{
		_ = Services.AddTransient<IHelp2PayRepository>(sp =>
			ActivatorUtilities.CreateInstance<THelp2PayRepository>(sp, redisDatabaseResolver(sp)));

		return this;
	}

	public OperatorConfiguration AddS88PayRedisRepository<TS88PayRepository>(
		Func<IServiceProvider, IDatabase> redisDatabaseResolver)
		where TS88PayRepository : class, IS88PayRepository
	{
		_ = Services.AddTransient<IS88PayRepository>(sp =>
			ActivatorUtilities.CreateInstance<TS88PayRepository>(sp, redisDatabaseResolver(sp)));

		return this;
	}

	public OperatorConfiguration AddMaintenanceRedisRepository<TMaintenanceRedisRepository>()
		where TMaintenanceRedisRepository : class, IMaintenanceRedisRepository
	{
		_ = Services.AddTransient<IMaintenanceRedisRepository, TMaintenanceRedisRepository>();

		return this;
	}

	public OperatorConfiguration AddPaymentServiceClient(Action<IHttpClientBuilder> configureHttpClient)
	{
		_ = Services.AddPaymentServiceClient(configureHttpClient);

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
		_ = Services.AddTransient<IStationLetterRepository, TStationLetterRepository>();

		return this;
	}

	public OperatorConfiguration AddSysRepository<TSysRepository>()
		where TSysRepository : class, ISysRepository
	{
		_ = Services.AddTransient<ISysRepository, TSysRepository>();

		return this;
	}

	public OperatorConfiguration AddVipLevelRepository<TVipLevelRepository>()
		where TVipLevelRepository : class, IVipLevelRepository
	{
		_ = Services.AddTransient<IVipLevelRepository, TVipLevelRepository>();

		return this;
	}

	public OperatorConfiguration AddWalletRepository<TWalletRepository>()
		where TWalletRepository : class, IPlayerRepository
	{
		_ = Services
			.AddTransient<IPlayerRepository, TWalletRepository>();

		return this;
	}

	public OperatorConfiguration ConfigureEmailConfiguration<TDep>(Action<EmailConfiguration, TDep> configure)
		where TDep : class
	{
		_ = Services
			.AddOptions<EmailConfiguration>()
			.Configure(configure);

		return this;
	}

	public OperatorConfiguration ConfigureOperatorOptions<TDep>(Action<OperatorOptions, TDep> configure)
		where TDep : class
	{
		_ = Services
			.AddOptions<OperatorOptions>()
			.Configure(configure);

		return this;
	}

	public OperatorConfiguration ConfigureWallet(Action<CashServiceClientOptions, IServiceProvider> configure)
	{
		_ = Services.AddCashServiceClient(configure);

		return this;
	}

	public OperatorConfiguration AddPlayerAgentRepository<T>()
		where T : class, IPlayerAgentRepository
	{
		_ = Services.AddTransient<IPlayerAgentRepository, T>()
			.Decorate<IPlayerAgentRepository, MemoryCachedPlayerAgentRepository>();

		return this;
	}

	public OperatorConfiguration AddMemoryCacheService<TMemoryCacheService>()
		where TMemoryCacheService : class, IMemoryCacheService
	{
		_ = Services.AddTransient<IMemoryCacheService, TMemoryCacheService>();

		return this;
	}
}