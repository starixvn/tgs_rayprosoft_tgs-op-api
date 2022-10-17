using System;
using Microsoft.Extensions.DependencyInjection;
using TGS.Services.Operations.CodeMappers;
using TGS.Services.Operations.Commands;
using TGS.Services.Operations.Queries;

namespace TGS.Services.Operations.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Dependency Injection of Operations, need to add the following dependencies beforehand:
    ///     1. AddRepository of Tgs database
    ///     2. AddRedis() in Infrastructures.Redis
    /// </summary>
    public static IServiceCollection AddOperations(this IServiceCollection services)
    {
		#region Code Mappers

		_ = services.AddScoped<IGameProviderCurrencyCodeMapper, GameProviderCurrencyCodeMapper>();
		_ = services.AddScoped<IGameProviderLanguageCodeMapper, GameProviderLanguageCodeMapper>();
		_ = services.AddScoped<IBrandCurrencyCodeMapper, BrandCurrencyCodeMapper>();
		_ = services.AddScoped<IBrandLanguageCodeMapper, BrandLanguageCodeMapper>();

        #endregion Code Mappers

        #region Queries

        services.AddFactory<IBrandQueries, BrandQueries>();
        services.AddFactory<IGameProviderQueries, GameProviderQueries>();
        services.AddFactory<IPlayerQueries, PlayerQueries>();
        services.AddFactory<IWalletQueries, WalletQueries>();
        services.AddFactory<IGameQueries, GameQueries>();

        services.AddFactory<IGameActionQueries, GameActionQueries>();
        services.AddFactory<IRoundQueries, RoundQueries>();

        services.AddFactory<IBetLimitQueries, BetLimitQueries>();

        #endregion Queries

        #region Commands

        services.AddFactory<IPlayerCommands, PlayerCommands>();

		#endregion Commands

		#region Operations

		_ = services.AddScoped<IBrandOperations, BrandOperations>();
		_ = services.AddScoped<IGameProviderOperations, GameProviderOperations>();
		_ = services.AddScoped<IPlayerOperations, PlayerOperations>();
		_ = services.AddScoped<IBrandGameProviderOperations, BrandGameProviderOperations>();

        #endregion Operations

        return services;
    }

    private static void AddFactory<TInterface, TServices>(this IServiceCollection services)
        where TInterface : class
        where TServices : class, TInterface
    {
		_ = services.AddScoped<TInterface, TServices>();
		_ = services.AddTransient<Func<TInterface>>(c => c.GetRequiredService<TInterface>);
    }
}
