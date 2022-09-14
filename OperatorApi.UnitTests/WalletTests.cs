using System;
using System.Threading;
using System.Threading.Tasks;
using ExpectedObjects;
using Moq;
using NUnit.Framework;
using Tgs.CashService.Client;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Services.Caches;
using TGS.Services.Operations;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Wallet;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class WalletTests
{
    [Test]
    public async Task Player_Can_Get_Balance_Info()
    {
        var moqCashServiceClient = new Mock<ICashServiceClient>();
        var moqPlayerOperations = new Mock<IPlayerOperations>();
        var moqPlayerOnlineRepository = new Mock<IPlayerOnlineRepository>();

        var sut = new GetBalanceQueryService(moqCashServiceClient.Object, moqPlayerOperations.Object, moqPlayerOnlineRepository.Object);

        var playerCode = "ThisIsPlayerCode";
        var brandId = Guid.NewGuid();
        moqPlayerOperations.Setup(fake => fake.GetCachedPlayerAsync(It.Is<string>(x => x == playerCode)))
            .Returns(Task.FromResult(new CachedPlayer
            {
                PlayerCode = playerCode,
                Username = "userName",
                BetLimitId = 0,
                BrandId = brandId,
                BrandCode = "BrandCode",
                DefaultWalletCurrencyCode = "CNY",
                DefaultWalletId = Guid.NewGuid(),
                ExternalCurrencyCode = "CNY",
                Status = PlayerStatus.Enabled,
                Type = PlayerType.RealPlayer
            }));
        var expected = new BalanceInfo(10000M).ToExpectedObject();

        moqCashServiceClient
            .Setup(fake => fake.GetBalanceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(10000M));

        moqPlayerOnlineRepository.Setup(fake => fake.SetAsync(brandId, playerCode));

        var actual = await sut.GetAsync(new GetBalanceQuery(playerCode));

        expected.ShouldMatch(actual);
    }
}
