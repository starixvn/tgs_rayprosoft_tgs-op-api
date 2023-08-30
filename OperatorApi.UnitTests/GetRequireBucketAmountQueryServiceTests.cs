using NSubstitute;
using Tgs.PlayerBucket.Client;
using Tgs.Service.PlayerBuckets;
using TGS.Services.Operator.ApplicationServices.Withdraw;

namespace TGS.Tests.OperatorApi.UnitTests;

public class GetRequireBucketAmountQueryServiceTests
{
	[Test]
	public async Task 取得玩家水桶餘額()
	{
		var fakePlayerBucketClient = Substitute.For<IPlayerBucketClient>();
		var fakePlayerBucketForTypeClient = Substitute.For<IPlayerBucketForTypesClient>();

		var sut = new GetRequireBucketAmountQueryService(
			fakePlayerBucketClient,
			fakePlayerBucketForTypeClient);

		var query = new GetRequireBucketAmountQuery(Guid.NewGuid(), "4ucmqplxb029");

		_ = fakePlayerBucketForTypeClient.ReadBucketsForBucketTypesAsync(
			Arg.Is(query.PlayerCode),
			Arg.Any<IEnumerable<string>>())
			.Returns(new[]{
				new TypeBucketResult(
					null,
					  new ReadBucketResult(12345M, Guid.NewGuid()))
			}.ToAsyncEnumerable());

		var actual = await sut.GetAsync(query);

		Assert.AreEqual(12345M, actual);
	}

	[Test]
	public async Task 取得玩家水桶餘額_如果回傳是空陣列_餘額回傳0()
	{
		var fakePlayerBucketClient = Substitute.For<IPlayerBucketClient>();
		var fakePlayerBucketForTypeClient = Substitute.For<IPlayerBucketForTypesClient>();

		var sut = new GetRequireBucketAmountQueryService(
			fakePlayerBucketClient,
			fakePlayerBucketForTypeClient);

		var query = new GetRequireBucketAmountQuery(Guid.NewGuid(), "4ucmqplxb029");

		var actual = await sut.GetAsync(query);

		Assert.AreEqual(0M, actual);
	}

	[Test]
	public async Task 取得玩家水桶餘額_會回傳所有桶中的最大值()
	{
		var fakePlayerBucketClient = Substitute.For<IPlayerBucketClient>();
		var fakePlayerBucketForTypeClient = Substitute.For<IPlayerBucketForTypesClient>();

		var sut = new GetRequireBucketAmountQueryService(
			fakePlayerBucketClient,
			fakePlayerBucketForTypeClient);

		var query = new GetRequireBucketAmountQuery(Guid.NewGuid(), "4ucmqplxb029");

		_ = fakePlayerBucketForTypeClient.ReadBucketsForBucketTypesAsync(
			Arg.Is(query.PlayerCode),
			Arg.Any<IEnumerable<string>>())
			.Returns(new[]{
				new TypeBucketResult(
					null,
					  new ReadBucketResult(100M, Guid.NewGuid())),
				new TypeBucketResult(
					"Game1",
					new ReadBucketResult(1000M, Guid.NewGuid())),
				new TypeBucketResult(
					"Game2",
					new ReadBucketResult(500M, Guid.NewGuid()))
			}.ToAsyncEnumerable());

		var actual = await sut.GetAsync(query);

		Assert.AreEqual(1000M, actual);
	}

	[Test]
	public async Task 取得玩家水桶餘額列表()
	{
		var fakePlayerBucketClient = Substitute.For<IPlayerBucketClient>();
		var fakePlayerBucketForTypeClient = Substitute.For<IPlayerBucketForTypesClient>();

		var sut = new GetRequireBucketAmountQueryService(
			fakePlayerBucketClient,
			fakePlayerBucketForTypeClient);

		var query = new GetRequireBucketAmountQuery(Guid.NewGuid(), "4ucmqplxb029");

		_ = await sut.QueryAsync(query).ToArrayAsync();

		_ = fakePlayerBucketClient.Received(1)
			.GetBrandBucketTypesAsync(Arg.Is(query.BrandId));
		_ = fakePlayerBucketForTypeClient.Received(1)
			.ReadBucketsForBucketTypesAsync(
				Arg.Is(query.PlayerCode),
				Arg.Any<IEnumerable<string>>());
	}
}
