using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Agent.Client;
using Tgs.Agent.Client.Models;
using TGS.Core.Models.Enums;
using Tgs.PlayerService.Client;
using Tgs.Report.Client;
using Tgs.Report.Client.Models;
using Tgs.Report.Client.Models.AgentWinLoss;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Report.AgentWinLossReport;

public class
	GetAgentWinLossQueryService(
		IReportClient reportClient,
		IAgentClient agentClient,
		IPlayerServiceClient playerClient)
	: IGetService<GetAgentWinLossQuery, GetAgentWinLossResponse>
{
	public async ValueTask<GetAgentWinLossResponse> GetAsync(GetAgentWinLossQuery request,
		CancellationToken cancellationToken = default)
	{
		var response =
			string.IsNullOrEmpty(request.Username) switch
			{
				true =>
					await GetDownLineAgentWinLossByMasterAgentIdAsync(request)
						.ConfigureAwait(false),
				false when request is { DownLine: false, MemberType: AccountType.Agent } =>
					await GetAgentWinLossAsync(request, cancellationToken).ConfigureAwait(false),
				false when request is { DownLine: true, MemberType: AccountType.Agent } =>
					await GetDownLineAgentWinLossAsync(request, cancellationToken)
						.ConfigureAwait(false),
				false when request.MemberType == AccountType.Player =>
					await GetPlayerWinLossAsync(request, cancellationToken)
						.ConfigureAwait(false),
				_ => null
			};

		if (response == null)
			return new GetAgentWinLossResponse { Valid = true, Items = [] };

		var pageArray = response.Items.Select(x
			=> new AgentWinLossModel
			{
				Id = x.Id,
				Username = x.Username,
				MemberType = x.MemberType,
				RoundCount = x.RoundCount,
				Turnover = x.Turnover,
				WinLoss = x.WinLoss,
				AgentWinLoss = x.AgentWinLoss,
				CompanyWinLoss = x.CompanyWinLoss,
				Special = x.Special,
				AgentSpecial = x.AgentSpecial,
				CompanySpecial = x.CompanySpecial,
				AgentTotal = x.AgentTotal,
				CompanyTotal = x.CompanyTotal
			});

		var total = new AgentWinLossTotalModel
		{
			RoundCount = response.Items.Sum(x => x.RoundCount),
			Turnover = response.Items.Sum(x => x.Turnover),
			WinLoss = response.Items.Sum(x => x.WinLoss),
			AgentWinLoss = response.Items.Sum(x => x.AgentWinLoss),
			CompanyWinLoss = response.Items.Sum(x => x.CompanyWinLoss),
			Special = response.Items.Sum(x => x.Special),
			AgentSpecial = response.Items.Sum(x => x.AgentSpecial),
			CompanySpecial = response.Items.Sum(x => x.CompanySpecial),
			AgentTotal = response.Items.Sum(x => x.AgentTotal),
			CompanyTotal = response.Items.Sum(x => x.CompanyTotal)
		};

		return new GetAgentWinLossResponse
		{
			Valid = true,
			TotalCount = response.TotalCount,
			Items = pageArray.ToArray(),
			Total = total
		};
	}

	private async Task<PagedResponse<Tgs.Report.Client.Models.AgentWinLoss.AgentWinLossModel>> GetAgentWinLossAsync(
		GetAgentWinLossQuery request,
		CancellationToken cancellationToken)
	{
		var childAgents =
			agentClient.GetChildAgentInfosAsync(new GetChildAgentInfosRequest(request.AgentId), cancellationToken);

		var agent = await agentClient
			.GetAgentInfoByUsernameAsync(new GetAgentInfoByUsernameRequest(request.BrandId, request.Username),
				cancellationToken).ConfigureAwait(false);

		var subAgent =
			await childAgents.FirstOrDefaultAsync(x => x.AgentId == agent?.Id, cancellationToken)
				.ConfigureAwait(false);

		var agentId = request.AgentId == agent?.Id ? request.AgentId : subAgent?.AgentId;

		return agentId == null
			? new PagedResponse<Tgs.Report.Client.Models.AgentWinLoss.AgentWinLossModel>()
			: await reportClient.GetAgentWinLossAsync(new GetAgentWinLossRequest
			{
				BrandId = request.BrandId,
				AgentId = agentId.Value,
				From = request.From,
				To = request.To,
				Page = request.Page,
				Rows = request.Rows,
				OrderBy = (short)request.OrderBy,
				SortBy = request.SortBy,
				ShowAll = request.ShowAll
			}).ConfigureAwait(false);
	}

	private async Task<PagedResponse<Tgs.Report.Client.Models.AgentWinLoss.AgentWinLossModel>>
		GetDownLineAgentWinLossAsync(
			GetAgentWinLossQuery request,
			CancellationToken cancellationToken)
	{
		var childAgents =
			agentClient.GetChildAgentInfosAsync(new GetChildAgentInfosRequest(request.AgentId), cancellationToken);

		var agent = await agentClient
			.GetAgentInfoByUsernameAsync(new GetAgentInfoByUsernameRequest(request.BrandId, request.Username),
				cancellationToken).ConfigureAwait(false);

		var subAgent =
			await childAgents.FirstOrDefaultAsync(x => x.AgentId == agent?.Id, cancellationToken)
				.ConfigureAwait(false);

		var agentId = request.AgentId == agent?.Id ? request.AgentId : subAgent?.AgentId;

		return agentId == null
			? new PagedResponse<Tgs.Report.Client.Models.AgentWinLoss.AgentWinLossModel>()
			: await reportClient.GetDownLineAgentWinLossAsync(new GetDownLineAgentWinLossRequest
			{
				BrandId = request.BrandId,
				AgentId = agentId.Value,
				From = request.From,
				To = request.To,
				Page = request.Page,
				Rows = request.Rows,
				OrderBy = (short)request.OrderBy,
				SortBy = request.SortBy,
				ShowAll = request.ShowAll
			}).ConfigureAwait(false);
	}

	private async Task<PagedResponse<Tgs.Report.Client.Models.AgentWinLoss.AgentWinLossModel>>
		GetDownLineAgentWinLossByMasterAgentIdAsync(
			GetAgentWinLossQuery request)
		=> await reportClient.GetDownLineAgentWinLossAsync(new GetDownLineAgentWinLossRequest
		{
			BrandId = request.BrandId,
			AgentId = request.AgentId,
			From = request.From,
			To = request.To,
			Page = request.Page,
			Rows = request.Rows,
			OrderBy = (short)request.OrderBy,
			SortBy = request.SortBy,
			ShowAll = request.ShowAll
		}).ConfigureAwait(false);

	private async Task<PagedResponse<Tgs.Report.Client.Models.AgentWinLoss.AgentWinLossModel>> GetPlayerWinLossAsync(
		GetAgentWinLossQuery request,
		CancellationToken cancellationToken)
	{
		var childAgents =
			agentClient.GetChildAgentInfosAsync(new GetChildAgentInfosRequest(request.AgentId), cancellationToken);

		var player =
			await playerClient.GetPlayerInfoByUserNameAsync(request.BrandId,
				request.Username,
				cancellationToken).ConfigureAwait(false);

		if (player == null)
			return new PagedResponse<Tgs.Report.Client.Models.AgentWinLoss.AgentWinLossModel>();

		var playerProperties =
			await playerClient.GetPlayerPropertiesAsync(player.PlayerCode,
				cancellationToken).ConfigureAwait(false);

		return Guid.TryParse(playerProperties["AgentId"], out var agentId) &&
			   (await childAgents.AnyAsync(x => x.AgentId == agentId, cancellationToken).ConfigureAwait(false) ||
				request.AgentId == agentId)
			? await reportClient.GetAgentPlayerWinLossAsync(new GetPlayerWinLossRequest
			{
				PlayerCode = player.PlayerCode,
				From = request.From,
				To = request.To,
				Page = request.Page,
				Rows = request.Rows,
				OrderBy = (short)request.OrderBy,
				SortBy = request.SortBy,
				ShowAll = request.ShowAll
			}).ConfigureAwait(false)
			: new PagedResponse<Tgs.Report.Client.Models.AgentWinLoss.AgentWinLossModel>();
	}
}