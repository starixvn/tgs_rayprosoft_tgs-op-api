using System;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using TGS.Core.Models.Enums;
using TGS.Services.Operations;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Game;
using TGS.Services.Operator.ApplicationServices.GameProvider;
using TGS.Services.Operator.GameProviders;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class GameProviderTests
{
	[Test]
	public async Task Can_Find_GpCode_By_SubGpCode()
	{
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();

		var sut = new GetGpCodeQueryService(
			fakeGameProviderRepository);

		var query = new GetGpCodeQuery(Guid.NewGuid(), "SubGpCode");

		_ = fakeGameProviderRepository
			.QueryAllSubGameProvidersAsync(Arg.Is(query.BrandId))
			.Returns(
				new[]
				{
					new BrandSubGameProviderInfo(
						"SubGpCode",
						"SubGpCode",
						"SubGpCode",
						Guid.NewGuid(),
						"GpCode",
						true,
						Status.Active,
						"{}")
				}.ToAsyncEnumerable());

		var expected = "GpCode";

		var actual = await sut.GetAsync(query);

		Assert.AreEqual(expected, actual.GpCode);
	}

	[Test]
	public async Task Get_GameProvider_List()
	{
		var brandId = Guid.NewGuid();

		var query = new GetGameProvidersQuery(
			brandId,
			"CNY",
			null,
			PlatformType.Desktop);

		var fakePlayerOperations = Substitute.For<IPlayerOperations>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();

		var evoGp = new GameProviderDataInfo(Guid.NewGuid(), "EVO", Status.Active, false);
		var sboGp = new GameProviderDataInfo(Guid.NewGuid(), "SBO", Status.Active, false);

		var subGameProviders = new[]
		{
			new BrandSubGameProviderInfo(
				"EVO",
				"EVO",
				"EVO",
				evoGp.Id,
				evoGp.Code,
				evoGp.IsTransferWallet,
				evoGp.Status,
				"{}"),
			new BrandSubGameProviderInfo(
				"NT",
				"NT",
				"NT",
				evoGp.Id,
				evoGp.Code,
				evoGp.IsTransferWallet,
				evoGp.Status,
				"{}"),
			new BrandSubGameProviderInfo(
				"RT",
				"RT",
				"RT",
				evoGp.Id,
				evoGp.Code,
				evoGp.IsTransferWallet,
				evoGp.Status,
				"{}"),
			new BrandSubGameProviderInfo(
				"SBO",
				"SBO",
				"SBO",
				sboGp.Id,
				sboGp.Code,
				sboGp.IsTransferWallet,
				sboGp.Status,
				"{}")
		};

		var games = new[]
		{
			new BrandGameInfo(
				1,
				"1",
				"1",
				"test",
				"EVO",
				evoGp.Id,
				"EVO",
				GameStatus.Active,
				GameType.LiveDealer,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0),
			new BrandGameInfo(
				2,
				"2",
				"2",
				"test",
				"RT",
				evoGp.Id,
				"EVO",
				GameStatus.Active,
				GameType.Slot,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0),
			new BrandGameInfo(
				3,
				"3",
				"3",
				"test",
				"NT",
				evoGp.Id,
				"EVO",
				GameStatus.Active,
				GameType.Slot,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0),
			new BrandGameInfo(
				4,
				"4",
				"4",
				"test",
				"SBO",
				sboGp.Id,
				"SBO",
				GameStatus.Active,
				GameType.Sportsbook,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0)
		};

		_ = fakeGameProviderRepository.QueryAllSubGameProvidersAsync(Arg.Is(brandId))
			.Returns(subGameProviders.ToAsyncEnumerable());

		_ = fakeGameProviderRepository.QueryAllGamesAsync(Arg.Is(brandId))
			.Returns(games.ToAsyncEnumerable());

		var sut = new GetGameProvidersQueryService(
			fakePlayerOperations,
			fakeGameProviderRepository);

		// act
		var actual = await sut.QueryAsync(query).ToArrayAsync();

		Assert.AreEqual(actual.Length, subGameProviders.Length);
	}

	[Test]
	public async Task Get_GameProvider_List_When_GameProvider_Is_Inactive()
	{
		var brandId = Guid.NewGuid();

		var query = new GetGameProvidersQuery(
			brandId,
			"CNY",
			null,
			PlatformType.Desktop);

		var fakePlayerOperations = Substitute.For<IPlayerOperations>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();

		var evoGp = new GameProviderDataInfo(Guid.NewGuid(), "EVO", Status.Active, false);
		var sboGp = new GameProviderDataInfo(Guid.NewGuid(), "SBO", Status.Inactive, false);

		var subGameProviders = new[]
		{
			new BrandSubGameProviderInfo(
				"EVO",
				"EVO",
				"EVO",
				evoGp.Id,
				evoGp.Code,
				evoGp.IsTransferWallet,
				evoGp.Status,
				"{}"),
			new BrandSubGameProviderInfo(
				"NT",
				"NT",
				"NT",
				evoGp.Id,
				evoGp.Code,
				evoGp.IsTransferWallet,
				evoGp.Status,
				"{}"),
			new BrandSubGameProviderInfo(
				"RT",
				"RT",
				"RT",
				evoGp.Id,
				evoGp.Code,
				evoGp.IsTransferWallet,
				evoGp.Status,
				"{}"),
			new BrandSubGameProviderInfo(
				"SBO",
				"SBO",
				"SBO",
				sboGp.Id,
				sboGp.Code,
				sboGp.IsTransferWallet,
				sboGp.Status,
				"{}")
		};

		var games = new[]
		{
			new BrandGameInfo(
				1,
				"1",
				"1",
				"test",
				"EVO",
				evoGp.Id,
				"EVO",
				GameStatus.Active,
				GameType.LiveDealer,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0),
			new BrandGameInfo(
				2,
				"2",
				"2",
				"test",
				"RT",
				evoGp.Id,
				"EVO",
				GameStatus.Active,
				GameType.Slot,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0),
			new BrandGameInfo(
				3,
				"3",
				"3",
				"test",
				"NT",
				evoGp.Id,
				"EVO",
				GameStatus.Active,
				GameType.Slot,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0),
			new BrandGameInfo(
				4,
				"4",
				"4",
				"test",
				"SBO",
				sboGp.Id,
				"SBO",
				GameStatus.Active,
				GameType.Sportsbook,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0)
		};

		_ = fakeGameProviderRepository.QueryAllSubGameProvidersAsync(Arg.Is(brandId))
			.Returns(subGameProviders.ToAsyncEnumerable());

		_ = fakeGameProviderRepository.QueryAllGamesAsync(Arg.Is(brandId))
			.Returns(games.ToAsyncEnumerable());

		var sut = new GetGameProvidersQueryService(
			fakePlayerOperations,
			fakeGameProviderRepository);

		// act
		var actual = await sut.QueryAsync(query).ToArrayAsync();

		// assert
		var expected = subGameProviders.Where(gp => gp.Status == Status.Active).ToArray();

		Assert.AreEqual(expected.Length, actual.Length);
	}

	[Test]
	public async Task Get_GameProvider_List_When_GameProvider_Is_UnderMaintenance()
	{
		var brandId = Guid.NewGuid();

		var query = new GetGameProvidersQuery(
			brandId,
			"CNY",
			null,
			PlatformType.Desktop);

		var fakePlayerOperations = Substitute.For<IPlayerOperations>();
		var fakeGameProviderRepository = Substitute.For<IGameProviderRepository>();

		var evoGp = new GameProviderDataInfo(Guid.NewGuid(), "EVO", Status.Maintenance, false);
		var sboGp = new GameProviderDataInfo(Guid.NewGuid(), "SBO", Status.Active, false);

		var subGameProviders = new[]
		{
			new BrandSubGameProviderInfo(
				"EVO",
				"EVO",
				"EVO",
				evoGp.Id,
				evoGp.Code,
				evoGp.IsTransferWallet,
				evoGp.Status,
				"{}"),
			new BrandSubGameProviderInfo(
				"NT",
				"NT",
				"NT",
				evoGp.Id,
				evoGp.Code,
				evoGp.IsTransferWallet,
				evoGp.Status,
				"{}"),
			new BrandSubGameProviderInfo(
				"RT",
				"RT",
				"RT",
				evoGp.Id,
				evoGp.Code,
				evoGp.IsTransferWallet,
				evoGp.Status,
				"{}"),
			new BrandSubGameProviderInfo(
				"SBO",
				"SBO",
				"SBO",
				sboGp.Id,
				sboGp.Code,
				sboGp.IsTransferWallet,
				sboGp.Status,
				"{}")
		};

		var games = new[]
		{
			new BrandGameInfo(
				1,
				"1",
				"1",
				"test",
				"EVO",
				evoGp.Id,
				"EVO",
				GameStatus.Active,
				GameType.LiveDealer,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0),
			new BrandGameInfo(
				2,
				"2",
				"2",
				"test",
				"RT",
				evoGp.Id,
				"EVO",
				GameStatus.Active,
				GameType.Slot,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0),
			new BrandGameInfo(
				3,
				"3",
				"3",
				"test",
				"NT",
				evoGp.Id,
				"EVO",
				GameStatus.Active,
				GameType.Slot,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0),
			new BrandGameInfo(
				4,
				"4",
				"4",
				"test",
				"SBO",
				sboGp.Id,
				"SBO",
				GameStatus.Active,
				GameType.Sportsbook,
				GamePlatformType.Desktop,
				true,
				string.Empty,
				"{}",
				GameTag.New,
				0)
		};

		_ = fakeGameProviderRepository.QueryAllSubGameProvidersAsync(Arg.Is(brandId))
			.Returns(subGameProviders.ToAsyncEnumerable());

		_ = fakeGameProviderRepository.QueryAllGamesAsync(Arg.Is(brandId))
			.Returns(games.ToAsyncEnumerable());

		var sut = new GetGameProvidersQueryService(
			fakePlayerOperations,
			fakeGameProviderRepository);

		// act
		var actual = await sut.QueryAsync(query).ToArrayAsync();

		// assert
		var expected = subGameProviders.Where(gp => gp.Status == Status.Maintenance).ToArray();
		var maintenanceGps = actual.Where(gp => gp.Status == Status.Maintenance).ToArray();

		Assert.AreEqual(expected.Length, maintenanceGps.Length);

		foreach (var sgp in expected)
		{
			var exists = maintenanceGps.Any(gp => gp.Code == sgp.Code);
			Assert.True(exists);
		}
	}
}
