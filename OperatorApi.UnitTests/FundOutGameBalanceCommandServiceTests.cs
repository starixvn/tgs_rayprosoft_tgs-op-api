using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Tgs.GameProviders.Pluggable.Client;
using TGS.Services.Operator.ApplicationServices.GameBalance;

namespace TGS.Tests.OperatorApi.UnitTests;
public class FundOutGameBalanceCommandServiceTests
{
	[Test]
	public async Task FundOutGameBalanceCommandService_轉出到遊戲中()
	{
		var fakeGameProvidersOperatorPluggableClient = Substitute.For<IGameProvidersOperatorPluggableClient>();
		var sut = new FundOutGameBalanceCommandService(
			fakeGameProvidersOperatorPluggableClient,
			NullLogger<FundOutGameBalanceCommandService>.Instance);

		var command = new FundOutGameBalanceCommand(
			"gp1",
			"test1",
			1000);

		await sut.ExecuteAsync(command);

		_ = fakeGameProvidersOperatorPluggableClient.Received(1)
			.GameProviderFundOutAsync(
				Arg.Is(command.PlayerCode),
				Arg.Is(command.GameProviderCode),
				Arg.Is<decimal>(command.Amount));
	}

	[Test]
	public async Task FundOutGameBalanceCommandService_轉出到遊戲中_如果轉出金額是0或負數不動作()
	{
		var fakeGameProvidersOperatorPluggableClient = Substitute.For<IGameProvidersOperatorPluggableClient>();
		var sut = new FundOutGameBalanceCommandService(
			fakeGameProvidersOperatorPluggableClient,
			NullLogger<FundOutGameBalanceCommandService>.Instance);

		var command = new FundOutGameBalanceCommand(
			"gp1",
			"test1",
			-1000);

		await sut.ExecuteAsync(command);

		_ = fakeGameProvidersOperatorPluggableClient.DidNotReceive()
			.GameProviderFundOutAsync(
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<decimal>());
	}
}
