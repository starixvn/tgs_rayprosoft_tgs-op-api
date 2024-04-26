using NSubstitute;
using Tgs.Player.All.Agent.Client;
using Tgs.Player.All.Agent.Client.Models;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.BrandAllAgent;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class BrandAllAgentTests
{
	[Test]
	public async Task Can_Get_Brand_All_Agent_Switch_On()
	{
		var fakeBrandAllAgentClient = Substitute.For<IBrandAllAgentClient>();

		var sut = new GetBrandAllAgentSwitchOnQueryService(fakeBrandAllAgentClient);

		var brandId = Guid.NewGuid();

		var query = new GetBrandAllAgentSwitchOnQuery(brandId);

		_ = await sut.GetAsync(query);

		_ = fakeBrandAllAgentClient.Received(1)
			.GetBrandAllAgentAsync(Arg.Is<GetBrandAllAgentRequest>(x => x.BrandId == brandId));
	}

	[Test]
	public async Task Can_Get_Brand_All_Agent()
	{
		var fakeBrandAllAgentClient = Substitute.For<IBrandAllAgentClient>();

		var sut = new GetBrandAllAgentTurnoverConditionsQueryService(fakeBrandAllAgentClient);

		var brandId = Guid.NewGuid();

		_ = fakeBrandAllAgentClient.GetBrandAllAgentAsync(Arg.Any<GetBrandAllAgentRequest>())
			.Returns(new BrandAllAgent(true, SettlementIntervalType.Daily));

		_ = fakeBrandAllAgentClient.GetGameTypesAsync(Arg.Any<GetGameTypesRequest>())
			.Returns(new[]
			{
				new GameTypeModel
				{
					Switch = true, AllAgentGameType = AllAgentGameType.CardGame, HasValidBet = true
				},
				new GameTypeModel
				{
					Switch = true, AllAgentGameType = AllAgentGameType.LiveDealer, HasValidBet = false
				}
			}.ToAsyncEnumerable());

		_ = fakeBrandAllAgentClient.GetAllValidBetConditionsAsync(Arg.Any<GetAllValidBetConditionsRequest>())
			.Returns(new[]
			{
				new ValidBetConditionModel()
				{
					AllAgentGameType = AllAgentGameType.CardGame, Rank = 1
				},
				new ValidBetConditionModel()
				{
					AllAgentGameType = AllAgentGameType.CardGame, Rank = 2
				},
				new ValidBetConditionModel()
				{
					AllAgentGameType = AllAgentGameType.LiveDealer, Rank = 1
				},
				new ValidBetConditionModel()
				{
					AllAgentGameType = AllAgentGameType.LiveDealer, Rank = 2
				}
			}.ToAsyncEnumerable());

		var query = new GetBrandAllAgentTurnoverConditionsQuery(brandId);

		var response = await sut.QueryAsync(query).ToArrayAsync();

		Assert.That(response.Length, Is.EqualTo(2));

		_ = fakeBrandAllAgentClient.Received(1)
			.GetBrandAllAgentAsync(Arg.Any<GetBrandAllAgentRequest>());
		_ = fakeBrandAllAgentClient.Received(1)
			.GetGameTypesAsync(Arg.Any<GetGameTypesRequest>());
		_ = fakeBrandAllAgentClient.Received(1)
			.GetAllValidBetConditionsAsync(Arg.Any<GetAllValidBetConditionsRequest>());
	}
}
