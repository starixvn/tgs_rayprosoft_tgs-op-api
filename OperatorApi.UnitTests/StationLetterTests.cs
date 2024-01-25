using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Tgs.Paging;
using TGS.Core.DataServices.Tgs;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Messages;
using TGS.Operator.Repositories;
using TGS.Services.Operator.ApplicationServices.StationLetters;
using TGS.Services.Operator.StationLetters;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class StationLetterTests
{
	[Test]
	public void Player_Can_Get_Station_Letters_Count()
	{
		var moqStationLetterRepository = new Mock<IStationLetterRepository>();

		var sut = new CountStationLettersQueryService(moqStationLetterRepository.Object);

		var playerCode = "ThisIsPlayerCode";

		var actual = sut.GetAsync(
			new GetStationLettersQuery(playerCode, new Paging<Services.Operator.StationLetters.StationLetter>()));

		moqStationLetterRepository.Verify(
			fake => fake.QueryStationLettersTotalCountAsync(It.Is<string>(x => x == playerCode)),
			Times.Exactly(1));
	}

	[Test]
	public async Task Player_Can_Get_Station_Letters_With_Status_Published()
	{
		var dbContextOptionsBuilder = new DbContextOptionsBuilder<TgsRepository>();
		_ = dbContextOptionsBuilder
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
		ITgsRepository repository = new TgsRepository(dbContextOptionsBuilder.Options);

		var playerCode = "playerCode";

		var stationLetterPlayers = new List<StationLetterPlayer>
		{
			new()
			{
				PlayerCode = playerCode,
				StationLetter = new Core.DataServices.Tgs.Models.StationLetter()
				{
					Status = StationLetterStatus.InVerify,
					Localization = "{}",
					CreatedByUsername = "System",
					UpdatedByUsername = "System"
				}
			},
			new()
			{
				PlayerCode = playerCode,
				StationLetter = new Core.DataServices.Tgs.Models.StationLetter()
				{
					Status = StationLetterStatus.Published,
					Localization = "{}",
					CreatedByUsername = "System",
					UpdatedByUsername = "System"
				}
			},
			new()
			{
				PlayerCode = playerCode,
				StationLetter = new Core.DataServices.Tgs.Models.StationLetter()
				{
					Status = StationLetterStatus.Rejected,
					Localization = "{}",
					CreatedByUsername = "System",
					UpdatedByUsername = "System"
				}
			}
		};

		await repository.StationLetterPlayers.AddRangeAsync(stationLetterPlayers);
		_ = await repository.SaveChangesAsync();

		var sut = new StationLetterRepository(repository);

		var actual = await sut.QueryStationLettersAsync(playerCode, new Paging<TGS.Services.Operator.StationLetters.StationLetter>() { Page = 0, RowCount = 50 }).ToListAsync();

		ClassicAssert.AreEqual(1, actual.Count);
	}

	[Test]
	public void Player_Can_List_Station_Letters()
	{
		var moqStationLetterRepository = new Mock<IStationLetterRepository>();

		var sut = new GetStationLettersQueryService(moqStationLetterRepository.Object);

		var playerCode = "ThisIsPlayerCode";

		var actual = sut.QueryAsync(
			new GetStationLettersQuery(playerCode, new Paging<Services.Operator.StationLetters.StationLetter>()));

		moqStationLetterRepository.Verify(
			fake => fake.QueryStationLettersAsync(It.Is<string>(x => x == playerCode), It.IsAny<Paging<TGS.Services.Operator.StationLetters.StationLetter>>()),
			Times.Exactly(1));
	}

	[Test]
	public void Player_Can_Set_Station_Letters_HaveRead()
	{
		var moqStationLetterRepository = new Mock<IStationLetterRepository>();

		var sut = new SetHaveReadCommandService(moqStationLetterRepository.Object);

		var playerCode = "ThisIsPlayerCode";
		var letterId = Guid.NewGuid();

		_ = sut.ExecuteAsync(new SetHaveReadCommand(playerCode, letterId));

		moqStationLetterRepository.Verify(
			fake => fake.SetStationLetterHaveReadAsync(It.Is<string>(x => x == playerCode), It.Is<Guid>(x => x == letterId)),
			Times.Exactly(1));
	}

	[Test]
	public async Task Player_Station_Letters_Should_Order_Desc_By_CreateOn()
	{
		// arrange
		var playerCode = "playerCode";
		var dbContextOptionsBuilder = new DbContextOptionsBuilder<TgsRepository>();
		_ = dbContextOptionsBuilder
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));

		var tgsRepository = new TgsRepository(dbContextOptionsBuilder.Options);

		var stationLetterPlayers = new List<StationLetterPlayer>();

		var stationLetter1 = new Core.DataServices.Tgs.Models.StationLetter()
		{
			Status = StationLetterStatus.Published,
			CreatedOn = new DateTimeOffset(new DateTime(2021, 4, 20)),
			Localization = "{}",
			CreatedByUsername = "System",
			UpdatedByUsername = "System"
		};
		stationLetterPlayers.Add(new StationLetterPlayer()
		{
			StationLetter = stationLetter1,
			PlayerCode = playerCode
		});

		var stationLetter2 = new Core.DataServices.Tgs.Models.StationLetter()
		{
			Status = StationLetterStatus.Published,
			CreatedOn = new DateTimeOffset(new DateTime(2021, 4, 25)),
			Localization = "{}",
			CreatedByUsername = "System",
			UpdatedByUsername = "System"
		};
		stationLetterPlayers.Add(new StationLetterPlayer()
		{
			StationLetter = stationLetter2,
			PlayerCode = playerCode
		});

		var stationLetter3 = new Core.DataServices.Tgs.Models.StationLetter()
		{
			Status = StationLetterStatus.Published,
			CreatedOn = new DateTimeOffset(new DateTime(2021, 4, 28)),
			Localization = "{}",
			CreatedByUsername = "System",
			UpdatedByUsername = "System"
		};
		stationLetterPlayers.Add(new StationLetterPlayer()
		{
			StationLetter = stationLetter3,
			PlayerCode = playerCode
		});

		await tgsRepository.StationLetterPlayers.AddRangeAsync(stationLetterPlayers);

		_ = await tgsRepository.SaveChangesAsync();

		var sut = new StationLetterRepository(tgsRepository);

		var excepted = new DateTimeOffset(new DateTime(2021, 4, 28));

		// act
		var r = await sut.QueryStationLettersAsync(playerCode).ToListAsync();

		// assert
		var actual = r.FirstOrDefault().CreateOn;
		ClassicAssert.AreEqual(excepted, actual);
	}

	[Test]
	public async Task Set_Db_Station_Letter_HaveRead_Should_Commit()
	{
		var dbContextOptionsBuilder = new DbContextOptionsBuilder<TgsRepository>();
		_ = dbContextOptionsBuilder
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));

		var tgsRepository = new TgsRepository(dbContextOptionsBuilder.Options);
		var sut = new StationLetterRepository(tgsRepository);

		var playerCode = "ThisIsPlayerCode";
		var letterId = Guid.NewGuid();

		_ = tgsRepository.StationLetters.Add(new Core.DataServices.Tgs.Models.StationLetter
		{
			Id = letterId,
			BrandId = Guid.NewGuid(),
			Subject = "test",
			Content = "content",
			Localization = "{}",
			CreatedByUsername = "System",
			UpdatedByUsername = "System"
		});

		var targetPlayerLetter = new StationLetterPlayer
		{
			StationLetterId = letterId,
			PlayerCode = playerCode,
			IsRead = false
		};

		_ = tgsRepository.StationLetterPlayers.Add(targetPlayerLetter);

		_ = await tgsRepository.SaveChangesAsync();

		await sut.SetStationLetterHaveReadAsync(playerCode, letterId);

		ClassicAssert.IsTrue(targetPlayerLetter.IsRead);
		ClassicAssert.IsNotNull(targetPlayerLetter.ReadOn);
	}
}
