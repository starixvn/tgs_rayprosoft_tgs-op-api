using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Agent.Client;
using Tgs.Agent.Client.Models;
using TGS.Core.Models.Enums;
using Tgs.PlayerService.Client;
using Tgs.Report.Client;
using Tgs.Report.Client.Models.AgentWinLoss;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.AgentWinLossReport;

public class
	GetAgentWinLossBreadcrumbQueryService(
		IReportClient reportClient,
		IAgentClient agentClient,
		IPlayerServiceClient playerServiceClient)
	: IGetService<GetAgentWinLossBreadcrumbQuery,
		IEnumerable<AgentWinLossBreadcrumb>>
{
	public async ValueTask<IEnumerable<AgentWinLossBreadcrumb>> GetAsync(
		GetAgentWinLossBreadcrumbQuery request,
		CancellationToken cancellationToken = default)
	{
		var childAgents =
			agentClient.GetChildAgentInfosAsync(new GetChildAgentInfosRequest(request.AgentId), cancellationToken);

		var agent = !string.IsNullOrEmpty(request.Username) && request.MemberType == AccountType.Agent
			? await childAgents
				.FirstOrDefaultAsync(x => x.AgentUsername == request.Username, cancellationToken).ConfigureAwait(false)
			: null;

		var player = !string.IsNullOrEmpty(request.Username) && request.MemberType == AccountType.Player
			? await playerServiceClient.GetPlayerInfoByUserNameAsync(request.BrandId,
				request.Username,
				cancellationToken).ConfigureAwait(false)
			: null;

		var reportRequest = new GetBreadcrumbRequest
		{
			AgentId = agent?.AgentId,
			PlayerCode = player?.PlayerCode,
			RoundId = request.RoundId
		};

		var response = await reportClient.GetAgentWinLossBreadcrumbAsync(reportRequest).ConfigureAwait(false);

		var haveSubAgents = await childAgents.AnyAsync(
			x => response.Select(r => r.Id).Contains(x.ParentAgentId.ToString()) ||
				 response.Select(r => r.Id).Contains(x.AgentId.ToString()),
			cancellationToken).ConfigureAwait(false);

		return haveSubAgents
			? response.Select(x => new AgentWinLossBreadcrumb { Username = x.Name, Id = x.Id, MemberType = x.Type })
			: [];
	}
}