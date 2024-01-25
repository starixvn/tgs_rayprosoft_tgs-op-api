#nullable enable

using NSubstitute;
using NUnit.Framework.Legacy;
using Tgs.DepositAccumulateRoulette.Clients;
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

		ClassicAssert.AreEqual(expected, actual);

		_ = fakeDepositAccumulateRouletteClient.Received(1)
			.RouletteAsync(
				Arg.Is(command.PromotionId),
				Arg.Is(command.PlayerCode),
				Arg.Any<CancellationToken>());
	}
}
