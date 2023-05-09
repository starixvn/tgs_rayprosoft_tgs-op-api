using NSubstitute;
using Tgs.StationLetterService.Client;
using TGS.Services.Operator.ApplicationServices.StationLetters;

namespace TGS.Tests.OperatorApi.UnitTests;

public class StationLetterV2Tests
{
	[Test]
	public async Task 回覆信件()
	{
		var fakeStationLetterClient = Substitute.For<IStationLetterClient>();

		var sut = new ReplyLetterCommandService(fakeStationLetterClient);

		var command = new ReplyLetterCommand("test1", Guid.NewGuid(), "content");

		_ = fakeStationLetterClient.GetStationLetterAsync(Arg.Is(command.LetterId))
			.Returns(new StationLetter(
				command.LetterId,
				"test2",
				command.PlayerCode,
				"tttt",
				null,
				"cccccc",
				null,
				DateTime.UtcNow,
				false,
				false));

		await sut.ExecuteAsync(command);

		_ = fakeStationLetterClient.Received(1)
			.ReplyStationLetterAsync(
				Arg.Is(command.LetterId),
				Arg.Is(new ReplyStationLetterRequest(
					command.PlayerCode,
					"test2",
					command.Content)));
	}

	[Test]
	public async Task 取得玩家站內信列表()
	{
		var fakeStationLetterClient = Substitute.For<IStationLetterClient>();

		var sut = new GetStationLettersV2QueryService(fakeStationLetterClient);

		var query = new GetStationLettersV2Query("test1");

		_ = await sut.QueryAsync(query).ToArrayAsync();

		_ = fakeStationLetterClient.Received(1)
			.ListStationLettersAsync(Arg.Is(query.PlayerCode));
	}

	[Test]
	public async Task 取得玩家寄件內容()
	{
		var fakeStationLetterClient = Substitute.For<IStationLetterClient>();

		var sut = new GetDeliveryLetterQueryService(fakeStationLetterClient);

		var query = new GetDeliveryLetterQuery(Guid.NewGuid());

		_ = await sut.GetAsync(query);

		_ = fakeStationLetterClient.Received(1)
			.GetDeliveryLetterAsync(Arg.Is(query.DeliveryId));
	}

	[Test]
	public async Task 取得玩家寄件備份列表()
	{
		var fakeStationLetterClient = Substitute.For<IStationLetterClient>();

		var sut = new GetDeliveryLettersQueryService(fakeStationLetterClient);

		var query = new GetDeliveryLettersQuery("test1");

		_ = await sut.QueryAsync(query).ToArrayAsync();

		_ = fakeStationLetterClient.Received(1)
			.ListDeliverLettersAsync(Arg.Is(query.PlayerCode));
	}

	[Test]
	public async Task 取得站內信內容()
	{
		var fakeStationLetterClient = Substitute.For<IStationLetterClient>();

		var sut = new GetStationLetterQueryService(fakeStationLetterClient);

		var query = new GetStationLetterQuery(Guid.NewGuid());

		_ = await sut.GetAsync(query);

		_ = fakeStationLetterClient.Received(1)
			.GetStationLetterAsync(Arg.Is(query.LetterId));
	}

	[Test]
	public async Task 設定信件已讀()
	{
		var fakeStationLetterClient = Substitute.For<IStationLetterClient>();

		var sut = new SetHaveReadV2CommandService(fakeStationLetterClient);

		var command = new SetHaveReadV2Command("test1", Guid.NewGuid());

		_ = fakeStationLetterClient.GetStationLetterAsync(Arg.Is(command.LetterId))
			.Returns(new StationLetter(
				command.LetterId,
				"test2",
				command.PlayerCode,
				"ttt",
				null,
				"ccccc",
				null,
				DateTime.UtcNow,
				false,
				false));

		await sut.ExecuteAsync(command);

		_ = fakeStationLetterClient.Received(1)
			.MarkStationLetterHaveReadAsync(Arg.Is(command.LetterId));
	}

	[Test]
	public async Task 發送信件()
	{
		var fakeStationLetterClient = Substitute.For<IStationLetterClient>();

		var sut = new SendStationLetterCommandService(fakeStationLetterClient);

		var command = new SendStationLetterCommand(
			"test1",
			new[] { "receiver1" },
			"abc",
			"asdfgh");

		await sut.ExecuteAsync(command);

		_ = fakeStationLetterClient.Received(1)
			.SendStationLetterAsync(Arg.Is(new StationLetterRequest(
				command.PlayerCode,
				command.Receivers,
				command.Subject,
				command.Content)));
	}
}
