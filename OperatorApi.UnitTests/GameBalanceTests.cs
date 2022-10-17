using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using Tgs.GameProviders.Pluggable.Client;
using TGS.Core.DataServices.Tgs;
using TGS.Core.Models;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Exceptions;
using TGS.Core.Models.Services.Caches;
using TGS.Core.Models.Tokens;
using TGS.Services.Operations;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.GameBalance;
using CoreModesl = TGS.Core.DataServices.Tgs.Models;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class GameBalanceTests
{
    [Test]
    public async Task Player_Can_Fund_In()
    {
        // arrange
        var fakeGameProvidersOperatorPluggableClient = Substitute.For<IGameProvidersOperatorPluggableClient>();
        var fakeLogger = new NullLogger<FundInGameBalanceCommandService>();

        var gameProviderCode = "KY";
        var playerCode = "playerCode";
        int amount = 10000;

        var sut = new FundInGameBalanceCommandService(fakeGameProvidersOperatorPluggableClient, fakeLogger);

        // act
        var command = new FundInGameBalanceCommand(gameProviderCode, playerCode, amount);
        await sut.ExecuteAsync(command, default);

        await fakeGameProvidersOperatorPluggableClient.Received(1)
            .GameProviderFundInAsync(
            playerCode,
            gameProviderCode,
            amount).ConfigureAwait(false);
    }

    [Test]
    public async Task Player_Can_Fund_Out()
    {
        // arrange
        var fakeGameProvidersOperatorPluggableClient = Substitute.For<IGameProvidersOperatorPluggableClient>();
        var fakeLogger = new NullLogger<FundOutGameBalanceCommandService>();

        var gameProviderCode = "KY";
        var playerCode = "playerCode";
        int amount = 10000;

        var sut = new FundOutGameBalanceCommandService(fakeGameProvidersOperatorPluggableClient, fakeLogger);

        // act
        var command = new FundOutGameBalanceCommand(gameProviderCode, playerCode, amount);
        await sut.ExecuteAsync(command, default);

        await fakeGameProvidersOperatorPluggableClient.Received(1)
            .GameProviderFundOutAsync(
            playerCode,
            gameProviderCode,
            amount).ConfigureAwait(false);
    }

    [Test]
    public async Task Player_Can_Get_Game_Balance_Infos()
    {
        var dbContextOptionsBuilder = new DbContextOptionsBuilder<TgsRepository>();
		_ = dbContextOptionsBuilder
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        ITgsRepository repository = new TgsRepository(dbContextOptionsBuilder.Options);
        var brandId = Guid.NewGuid();

		_ = repository.Brands.Add(new CoreModesl.Brand()
		{
			Id = brandId,
			Name = "brandName",
			ApiKey = string.Empty,
			ClientId = string.Empty,
			Code = "Test",
			CreatedByUsername = "System",
			UpdatedByUsername = "System",
			SecretKey = string.Empty
		});
		_ = await repository.SaveChangesAsync();

        var sut = new GetGameBalanceInfosQueryService(repository);

        var query = new GetGameBalanceInfosQuery(
            BrandId: brandId,
            PlayerCode: "playerCode",
            RecordOnRange: null);

        var info = await sut.QueryAsync(query).ToArrayAsync();

        Assert.IsTrue(info.Length >= 0);
    }

    [Test]
    public async Task Player_Can_Get_Transfer_Wallet_Balance()
    {
        // arrange
        var playerCode = "playerCode";
        var gameProviderCode = "KY";
        var fakeUri = new Uri("https://www.fake.com");

        var fakeGameProvidersOperatorPluggableClient = Substitute.For<IGameProvidersOperatorPluggableClient>();

		_ = fakeGameProvidersOperatorPluggableClient
			.GetBalanceAsync(playerCode, gameProviderCode)
			.Returns(1000m);

        var sut = new GetGameBalanceQueryService(fakeGameProvidersOperatorPluggableClient);

        // act
        var query = new GetGameBalanceQuery(playerCode, gameProviderCode);
		_ = await sut.GetAsync(query, default);

        // assert
        _ = fakeGameProvidersOperatorPluggableClient
            .Received(1)
            .GetBalanceAsync(Arg.Is(playerCode), Arg.Is(gameProviderCode));
    }

    [Test]
    public void Player_Can_Not_Use_Transfer_Wallet_Balance_Query_FundIn_FundOut_When_Brand_GameProvider_Status_Is_Inactive()
    {
        // arrange
        var fakeGameProviderOperations = Substitute.For<IGameProviderOperations>();
        var fakeBrandOperations = Substitute.For<IBrandOperations>();
        var playerCode = "playerCode";
        var brandId = Guid.NewGuid();
        var gameProviderCode = "ae5533bd-35d5-5478-19d6-233ed44f7c9d";
        var gameProviderId = Guid.NewGuid();
        var gameProviderStatus = Status.Active;

        _ = fakeGameProviderOperations
            .GetGameProviderIdByCodeAsync(Arg.Any<string>())
            .Returns(gameProviderId);
        _ = fakeGameProviderOperations
            .GetGameProviderStatusAsync(Arg.Any<Guid>())
            .Returns(gameProviderStatus);

        var playerTokenData = new PlayerTokenData
        {
            PlayerCode = playerCode
        };

        var brandGameProviderStatus = Status.Inactive;

        _ = fakeBrandOperations.GetBrandGameProviderStatusAsync(brandId, gameProviderId).Returns(brandGameProviderStatus);

        var sut = new GameProviderStatusValidator(
            fakeGameProviderOperations,
            fakeBrandOperations);

        // assert
        _ = Assert.ThrowsAsync<InvalidStatusException>(async () => await sut.CheckGameProviderStatusAsync(playerTokenData, gameProviderCode, brandId));
    }

    [Test]
    public void Player_Can_Not_Use_Transfer_Wallet_Balance_Query_FundIn_FundOut_When_Brand_GameProvider_Status_Is_Maintenance()
    {
        // arrange
        var fakeGameProviderOperations = Substitute.For<IGameProviderOperations>();
        var fakeBrandOperations = Substitute.For<IBrandOperations>();
        var playerCode = "playerCode";
        var brandId = Guid.NewGuid();
        var gameProviderCode = "ae5533bd-35d5-5478-19d6-233ed44f7c9d";
        var gameProviderId = Guid.NewGuid();
        var gameProviderStatus = Status.Active;

        _ = fakeGameProviderOperations
            .GetGameProviderIdByCodeAsync(Arg.Any<string>())
            .Returns(gameProviderId);
        _ = fakeGameProviderOperations
            .GetGameProviderStatusAsync(Arg.Any<Guid>())
            .Returns(gameProviderStatus);

        var playerTokenData = new PlayerTokenData
        {
            PlayerCode = playerCode
        };

        var brandGameProviderStatus = Status.Maintenance;

        _ = fakeBrandOperations.GetBrandGameProviderStatusAsync(brandId, gameProviderId).Returns(brandGameProviderStatus);

        var sut = new GameProviderStatusValidator(fakeGameProviderOperations, fakeBrandOperations);

        // assert
        _ = Assert.ThrowsAsync<GameProviderUnderMaintenanceException>(async () => await sut.CheckGameProviderStatusAsync(playerTokenData, gameProviderCode, brandId));
    }

    [Test]
    public void Player_Can_Not_Use_Transfer_Wallet_Balance_Query_FundIn_FundOut_When_GameProvider_Status_Is_Inactive()
    {
        // arrange
        var fakeGameProviderOperations = Substitute.For<IGameProviderOperations>();
        var fakeBrandOperations = Substitute.For<IBrandOperations>();
        var playerCode = "playerCode";
        var brandId = Guid.NewGuid();
        var gameProviderCode = "ae5533bd-35d5-5478-19d6-233ed44f7c9d";
        var gameProviderId = Guid.NewGuid();
        var gameProviderStatus = Status.Inactive;

        _ = fakeGameProviderOperations
            .GetGameProviderIdByCodeAsync(Arg.Any<string>())
            .Returns(gameProviderId);
        _ = fakeGameProviderOperations
            .GetGameProviderStatusAsync(Arg.Any<Guid>())
            .Returns(gameProviderStatus);

        var playerTokenData = new PlayerTokenData
        {
            PlayerCode = playerCode
        };

        var sut = new GameProviderStatusValidator(fakeGameProviderOperations, fakeBrandOperations);

        // assert
        _ = Assert.ThrowsAsync<InvalidStatusException>(async () => await sut.CheckGameProviderStatusAsync(playerTokenData, gameProviderCode, brandId));
    }

    [Test]
    public void Player_Can_Not_Use_Transfer_Wallet_Balance_Query_FundIn_FundOut_When_GameProvider_Status_Is_Maintenance()
    {
        // arrange
        var fakeGameProviderOperations = Substitute.For<IGameProviderOperations>();
        var fakeBrandOperations = Substitute.For<IBrandOperations>();
        var playerCode = "playerCode";
        var brandId = Guid.NewGuid();
        var gameProviderCode = "ae5533bd-35d5-5478-19d6-233ed44f7c9d";
        var gameProviderId = Guid.NewGuid();
        var gameProviderStatus = Status.Maintenance;

        _ = fakeGameProviderOperations
            .GetGameProviderIdByCodeAsync(Arg.Any<string>())
            .Returns(gameProviderId);
        _ = fakeGameProviderOperations
            .GetGameProviderStatusAsync(Arg.Any<Guid>())
            .Returns(gameProviderStatus);

        var playerTokenData = new PlayerTokenData
        {
            PlayerCode = playerCode
        };

        var sut = new GameProviderStatusValidator(fakeGameProviderOperations, fakeBrandOperations);

        // assert
        _ = Assert.ThrowsAsync<GameProviderUnderMaintenanceException>(async ()
            => await sut.CheckGameProviderStatusAsync(playerTokenData, gameProviderCode, brandId));
    }
}
