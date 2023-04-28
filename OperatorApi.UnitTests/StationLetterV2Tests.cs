using NSubstitute;
using Tgs.StationLetterService.Client;
using TGS.Services.Operator.ApplicationServices.StationLetters;

namespace TGS.Tests.OperatorApi.UnitTests;

public class StationLetterV2Tests
{
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
	public async Task 取得站內信內容()
	{
		var fakeStationLetterClient = Substitute.For<IStationLetterClient>();

		var sut = new GetStationLetterQueryService(fakeStationLetterClient);

		var query = new GetStationLetterQuery(Guid.NewGuid());

		_ = await sut.GetAsync(query);

		_ = fakeStationLetterClient.Received(1)
			.GetStationLetterAsync(Arg.Is(query.LetterId));
	}
}
