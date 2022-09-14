#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Tgs.DepositAccumulateRoulette.Clients;
using Tgs.RouletteServices.Clients;
using TGS.Services.Operator.ApplicationServices.Promotions;
using RouletteResult = Tgs.DepositAccumulateRoulette.Clients.RouletteResult;

namespace TGS.Tests.OperatorApi.UnitTests;

public class PromotionTests
{
    [Test]
    public async Task Deposit_Accumulate_Roulette_Should_Get_Result()
    {
        var fakeDepositAccumulateRouletteClient = Substitute.For<IDepositAccumulateRouletteClient>();

        var command = new PlayDepositAccumulateRouletteCommand(Guid.NewGuid(), "player1");

        var expected = new RouletteResult(Guid.NewGuid(), 3);

        _ = fakeDepositAccumulateRouletteClient
            .RouletteAsync(
                Arg.Is(command.PromotionId),
                Arg.Is(command.PlayerCode),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var sut = new PlayDepositAccumulateRouletteCommandService(fakeDepositAccumulateRouletteClient);

        var actual = await sut.GetAsync(command);

        Assert.AreEqual(expected, actual);

        _ = fakeDepositAccumulateRouletteClient.Received(1)
            .RouletteAsync(
                Arg.Is(command.PromotionId),
                Arg.Is(command.PlayerCode),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Get_Roulette_Record()
    {
        var fakeRouletteSignInClient = Substitute.For<IRouletteSignInClient>();
        var sut = new GetSignInRouletteRecordQueryService(fakeRouletteSignInClient);

        var query = new GetSignInRouletteRecordQuery("player1", Guid.NewGuid());

        await sut.GetAsync(query);

        _ = fakeRouletteSignInClient.Received(1)
            .GetRouletteRecordAsync(
                Arg.Is(query.PromotionId),
                Arg.Is(query.PlayerCode),
                Arg.Any<DateOnly>());
    }

    [Test]
    public async Task Roulette_Should_Get_Result()
    {
        var fakeRouletteSignInClient = Substitute.For<IRouletteSignInClient>();
        var sut = new SignInRouletteCommandService(fakeRouletteSignInClient);

        var command = new SignInRouletteCommand("player1", Guid.NewGuid());

        await sut.GetAsync(command);

        _ = fakeRouletteSignInClient.Received(1)
            .RouletteAsync(
                Arg.Is(command.PromotionId),
                Arg.Is(command.PlayerCode),
                Arg.Any<DateOnly>());
    }
}
