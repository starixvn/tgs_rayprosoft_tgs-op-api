using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Tgs.Report.Client;
using Tgs.Report.Client.Models;
using Tgs.ServiceUtility;
using Tgs.Report.Client.Enum;
using Tgs.Report.Client.Models.Agent;

namespace TGS.Services.Operator.ApplicationServices.Report.AgentContributionReport;

public class
	GetAgentContributionQueryService(
		IReportClient reportClient,
		IMemoryCache memoryCache)
	: IGetService<GetAgentContributionQuery, PagedResponse<AgentContributionModel>>
{
	public async ValueTask<PagedResponse<AgentContributionModel>> GetAsync(GetAgentContributionQuery request,
	CancellationToken cancellationToken = default)
	{
		var key =
			$"contributions:{request.BrandId}:{request.AgentId}:{request.From.ToUnixTimeMilliseconds()}:{request.To.ToUnixTimeMilliseconds()}";

		if (memoryCache.TryGetValue(key, out PagedResponse<AgentContributionModel>? response))
			return response!;

		var contributionRequest = new GetAgentContributionsRequest
		{
			BrandId = request.BrandId,
			AgentId = request.AgentId,
			IncludeAgents = true,
			From = request.From,
			To = request.To,
			Page = 1,
			Rows = int.MaxValue,
			OrderBy = request.IsDesc ? (short)SortOrder.Descending : (short)SortOrder.Ascending,
			SortBy = request.SortBy,
			ShowAll = true
		};

		response = await reportClient.GetAgentContributionsAsync(contributionRequest);

		_ = memoryCache.Set(key, response, TimeSpan.FromMinutes(5));

		return response;
	}
}