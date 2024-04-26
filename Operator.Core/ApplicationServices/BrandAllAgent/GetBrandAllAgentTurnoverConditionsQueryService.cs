using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Tgs.Player.All.Agent.Client;
using Tgs.Player.All.Agent.Client.Models;
using Tgs.ServiceUtility;
using AllAgentGameType = TGS.Core.Models.Enums.AllAgentGameType;

namespace TGS.Services.Operator.ApplicationServices.BrandAllAgent;

internal class GetBrandAllAgentTurnoverConditionsQueryService(IBrandAllAgentClient client) : IQueryService<GetBrandAllAgentTurnoverConditionsQuery, BrandAllAgentTurnover>
{
	public async IAsyncEnumerable<BrandAllAgentTurnover> QueryAsync(
		GetBrandAllAgentTurnoverConditionsQuery query,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var brandAllAgent =
			await client.GetBrandAllAgentAsync(
				new GetBrandAllAgentRequest(query.BrandId));

		if (!brandAllAgent.Switch)
		{
			yield return new BrandAllAgentTurnover();
		}

		var brandAllAgentGameTypes = await client
			.GetGameTypesAsync(new GetGameTypesRequest(query.BrandId))
			.ToArrayAsync(cancellationToken);

		var validGameTypes = brandAllAgentGameTypes.Where(x => x is { Switch: true, HasValidBet: true })
			.Select(x => x.AllAgentGameType);

		var conditions = await client
			.GetAllValidBetConditionsAsync(new GetAllValidBetConditionsRequest(query.BrandId))
			.Where(x => validGameTypes.Contains(x.AllAgentGameType))
			.Select(x => new BrandAllAgentTurnover
			{
				Rank = x.Rank,
				AllAgentGameType = (AllAgentGameType)x.AllAgentGameType,
				Turnover = x.NetProfit,
				ActiveNumber = x.ActiveNumber,
				RebatePercentage = x.RebatePercentage
			}).ToArrayAsync(cancellationToken);

		foreach (var condition in conditions)
		{
			yield return condition;
		}
	}
}