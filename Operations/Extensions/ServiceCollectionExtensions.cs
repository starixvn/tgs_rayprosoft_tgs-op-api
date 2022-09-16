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

        services.AddScoped<IGameProviderCurrencyCodeMapper, GameProviderCurrencyCodeMapper>();
        services.AddScoped<IGameProviderLanguageCodeMapper, GameProviderLanguageCodeMapper>();
        services.AddScoped<IBrandCurrencyCodeMapper, BrandCurrencyCodeMapper>();
        services.AddScoped<IBrandLanguageCodeMapper, BrandLanguageCodeMapper>();

        #endregion Code Mappers

        #region Queries

        services.AddFactory<IBrandQueries, BrandQueries>();
        services.AddFactory<IGameProviderQueries, GameProviderQueries>();
        services.AddFactory<IAgentQueries, AgentQueries>();
        services.AddFactory<IPlayerQueries, PlayerQueries>();
        services.AddFactory<IWalletQueries, WalletQueries>();
        services.AddFactory<IGameQueries, GameQueries>();

        services.AddFactory<IGameActionQueries, GameActionQueries>();
        services.AddFactory<IRoundQueries, RoundQueries>();

        services.AddFactory<IBetLimitQueries, BetLimitQueries>();

        #endregion Queries

        #region Commands

        services.AddFactory<IPlayerCommands, PlayerCommands>();
        services.AddFactory<IAgentCommands, AgentCommands>();

        #endregion Commands

        #region Operations

        services.AddScoped<IBrandOperations, BrandOperations>();
        services.AddScoped<IGameProviderOperations, GameProviderOperations>();
        services.AddScoped<IPlayerOperations, PlayerOperations>();
        services.AddScoped<IBrandGameProviderOperations, BrandGameProviderOperations>();

        #endregion Operations

        return services;
    }

    private static void AddFactory<TInterface, TServices>(this IServiceCollection services)
        where TInterface : class
        where TServices : class, TInterface
    {
        services.AddScoped<TInterface, TServices>();
        services.AddTransient<Func<TInterface>>(c => c.GetRequiredService<TInterface>);
    }
}
