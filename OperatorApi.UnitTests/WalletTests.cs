using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpectedObjects;
using Moq;
using NSubstitute;
using NUnit.Framework;
using Tgs.CashService.Client;
using Tgs.PlayerService.Client;
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
		_ = moqPlayerOperations.Setup(fake => fake.GetCachedPlayerAsync(It.Is<string>(x => x == playerCode)))
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
				Status = Core.Models.Enums.PlayerStatus.Enabled,
				Type = Core.Models.Enums.PlayerType.RealPlayer
			}));
		var expected = new BalanceInfo(10000M).ToExpectedObject();

		_ = moqCashServiceClient
			.Setup(fake => fake.GetBalanceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.Returns(ValueTask.FromResult(10000M));

		_ = moqPlayerOnlineRepository.Setup(fake => fake.SetAsync(brandId, playerCode));

		var actual = await sut.GetAsync(new GetBalanceQuery(playerCode));

		expected.ShouldMatch(actual);
	}

	[Test]
	public async Task 取得玩家帳變紀錄()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();

		var sut = new GetPlayerTransactionLogsQueryService(
			fakePlayerServiceClient,
			fakeCashServiceClient);

		var query = new GetPlayerTransactionLogsQuery(
			"test1",
			DateTime.Now.AddDays(-1D),
			DateTime.Now.AddDays(1D),
			1,
			10);

		var walletId = Guid.NewGuid();

		_ = fakePlayerServiceClient.GetPlayerPropertiesAsync(Arg.Is(query.PlayerCode))
			.Returns(new Dictionary<string, string>
			{
				["DefaultWalletId"] = walletId.ToString()
			}.ToImmutableDictionary());

		var expectedPagingCondition = new PagingConditions(1, 10);

		_ = await sut.GetAsync(query);

		_ = fakeCashServiceClient.Received(1)
			.QueryTransactionLogsAsync(
				Arg.Is(walletId),
				Arg.Is(query.From),
				Arg.Is(query.To),
				Arg.Is<IEnumerable<int>>(x => x.SequenceEqual(new[] { 6, 4100, 3800, 3850, 3860, 4 })),
				Arg.Is(expectedPagingCondition));
	}
}
