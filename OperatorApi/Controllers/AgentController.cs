using Microsoft.AspNetCore.Mvc;
using Tgs.Agent.Client.Models;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Models.Messages;
using Tgs.PlayerService.Client;
using TGS.Services.Agent;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Agent;
using TGS.Services.Operator.ApplicationServices.Player;
using TGS.Services.Operator.ApplicationServices.PlayerPayment;
using Tgs.VipLevelService.Clients;
using PlayerStatus = TGS.Core.Models.Enums.PlayerStatus;
using Tgs.Agent.Client;
using TGS.Core.Models.Enums;


#nullable enable

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AgentController : ControllerBase
{
	[HttpGet(Name = "GetInvitationAgentInfo")]
	public async Task<AgentInvitationInfoResponse?> GetInvitationAgentInfoAsync(
		[FromServices] IGetService<GetAgentInfoByInvitationCodeQuery, AgentInfo?> getService,
		[FromHeader(Name = "Agent-Invitation-Code")] string? invitationCode)
	{
		if (string.IsNullOrWhiteSpace(invitationCode))
			return null;

		var agentInfo = await getService.GetAsync(
			new GetAgentInfoByInvitationCodeQuery(invitationCode),
			HttpContext.RequestAborted).ConfigureAwait(false);

		return agentInfo is not null
			? new AgentInvitationInfoResponse
			{
				AgentId = agentInfo.Id,
				UserName = agentInfo.UserName,
				InvitationCode = agentInfo.InvitationCode
			}
			: null;
	}

	[HttpGet("info", Name = "GetAgentInfo")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<AgentInfoResponse?> GetAgentInfoAsync(
		[FromServices] IGetService<GetAgentInfoQuery, AgentInfoModel?> getAgentInfoService,
		[FromServices] IGetService<GetNegativeAgentDividendQuery, decimal> getNegativeAgentDividendService,
		[FromServices] IGetService<GetAgentProductFeeInfoQuery, AgentProductFeeInfo?> getAgentProductFeeInfoService,
		[FromServices] IGetService<GetAgentTeamInfoQuery, GetAgentTeamInfoModel?> getAgentTeamInfoService,
		[FromServices] IGetService<GetPlayerAgentQuery, Guid?> playerAgentService)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var agentId =
			await playerAgentService.GetAsync(new GetPlayerAgentQuery(playerTokenData!.PlayerCode), HttpContext.RequestAborted);

		if (agentId is null)
		{
			return null;
		}

		var agentInfo = await getAgentInfoService.GetAsync(
			new GetAgentInfoQuery(agentId.Value),
			HttpContext.RequestAborted).ConfigureAwait(false);

		var negativeAgentDividend =
			await getNegativeAgentDividendService.GetAsync(new GetNegativeAgentDividendQuery(agentId.Value));

		var agentProductFeeInfo =
			await getAgentProductFeeInfoService.GetAsync(new GetAgentProductFeeInfoQuery(agentId.Value));

		var agentTeamInfo = await getAgentTeamInfoService.GetAsync(new GetAgentTeamInfoQuery(brandId, agentId.Value));

		return agentInfo is not null
			? new AgentInfoResponse
			{
				DividendPercentage = agentInfo.DividendPercentage,
				NegativeAgentDividend = negativeAgentDividend,
				CardGamePercentage = agentProductFeeInfo?.CurrentCardGamePercentage ?? 0M,
				LiveDealerPercentage = agentProductFeeInfo?.CurrentLiveDealerPercentage ?? 0M,
				LotteryPercentage = agentProductFeeInfo?.CurrentLotteryPercentage ?? 0M,
				SlotPercentage = agentProductFeeInfo?.CurrentSlotPercentage ?? 0M,
				SportsPercentage = agentProductFeeInfo?.CurrentSportsPercentage ?? 0M,
				VirtualSportsPercentage = agentProductFeeInfo?.CurrentVirtualSportsPercentage ?? 0M,
				DirectAgentCount = agentTeamInfo?.DirectAgentCount ?? 0,
				IndirectAgentCount = agentTeamInfo?.IndirectAgentCount ?? 0,
				DirectPlayerCount = agentTeamInfo?.DirectPlayerCount ?? 0,
				IndirectPlayerCount = agentTeamInfo?.IndirectPlayerCount ?? 0,
				InvitationCode = agentInfo.InvitationCode,
				NegativeAgentDividendCarryForward = agentInfo.NegativeAgentDividendCarryForward
			}
			: null;
	}

	[HttpGet("direct-player", Name = "GetDirectPlayers")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<PagedResponse<DirectPlayersResponse>> GetDirectPlayersAsync(
		[FromServices] IGetService<GetPlayerAgentQuery, Guid?> playerAgentService,
		[FromServices] IGetService<GetPlayerPaymentOverviewQuery, PlayerPaymentOverview> playerPaymentService,
		[FromServices] IQueryService<GetDirectPlayersQuery, PlayerListItem> directPlayersService,
		[FromServices] IVipLevelServiceClient vipLevelServiceClient,
		[FromServices] IPlayerServiceClient playerServiceClient,
		[FromQuery] GetDirectPlayersRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var agentId =
			await playerAgentService.GetAsync(new GetPlayerAgentQuery(playerTokenData!.PlayerCode),
				HttpContext.RequestAborted);

		if (agentId is null)
		{
			return new PagedResponse<DirectPlayersResponse>
			{
				Valid = false
			};
		}

		var directPlayers = await directPlayersService
			.QueryAsync(new GetDirectPlayersQuery(brandId, agentId!.Value, request.PlayerUsername, request.PlayerStatus))
			.Where(x => x.Status != (Tgs.PlayerService.Client.PlayerStatus)PlayerStatus.RegisterUndone)
			.OrderByDescending(x => x.RegisteredOn)
			.ToArrayAsync();

		var response = await directPlayers
			.Chunk(request.Rows).Skip(request.Page - 1)
			.SelectMany(x => x)
			.ToAsyncEnumerable()
			.SelectAwait(async x =>
			{
				var totalDepositWithdraw =
					await playerPaymentService.GetAsync(new GetPlayerPaymentOverviewQuery(x.PlayerCode));

				var playerVipLevel = await vipLevelServiceClient.PlayerVipLevelGetAsync(
						x.PlayerCode,
						HttpContext.RequestAborted)
					.ConfigureAwait(false);

				var loginInfo = await playerServiceClient.GetPlayerLoginInfoAsync(
					x.PlayerCode,
					HttpContext.RequestAborted).ConfigureAwait(false);

				return new DirectPlayersResponse
				{
					PlayerUsername = x.UserName,
					PlayerStatus = (PlayerStatus)x.Status,
					VipLevel = playerVipLevel.VipLevel,
					RegisteredOn = x.RegisteredOn,
					LastLoginOn = loginInfo?.LoginOn,
					TotalDepositAmount = totalDepositWithdraw.TotalDepositAmount,
					TotalWithdrawAmount = totalDepositWithdraw.TotalWithdrawAmount
				};
			})
			.ToArrayAsync(HttpContext.RequestAborted);

		return new PagedResponse<DirectPlayersResponse>
		{
			Valid = true,
			TotalCount = directPlayers.Length,
			Items = response
		};
	}

	[HttpGet("direct-agent", Name = "GetDirectAgents")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<PagedResponse<DirectAgentsResponse>> GetDirectAgentsAsync(
		[FromServices] IGetService<GetPlayerAgentQuery, Guid?> playerAgentService,
		[FromServices] IPlayerServiceClient playerServiceClient,
		[FromServices] IAgentClient agentClient,
		[FromQuery] GetDirectAgentsRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var agentId =
			await playerAgentService.GetAsync(new GetPlayerAgentQuery(playerTokenData!.PlayerCode),
				HttpContext.RequestAborted);

		if (agentId is null)
		{
			return new PagedResponse<DirectAgentsResponse>
			{
				Valid = false
			};
		}

		var childAgentInfos = await agentClient
			.GetChildAgentInfosAsync(new GetChildAgentInfosRequest(agentId.Value), HttpContext.RequestAborted)
			.ToArrayAsync(HttpContext.RequestAborted);

		var agentPlayerCounts =
			await playerServiceClient
				.GetPlayerCountInfosByBrandIdAsync(brandId, false, HttpContext.RequestAborted)
				.ToArrayAsync(HttpContext.RequestAborted);

		var directAgents = childAgentInfos
			.Where(x => x.ParentAgentId == agentId.Value)
			.Where(x => string.IsNullOrEmpty(request.AgentUsername) ||
			            x.AgentUsername.Contains(request.AgentUsername, StringComparison.OrdinalIgnoreCase))
			.Where(x => request.AgentStatus == null ||
			            x.Status == (Tgs.Agent.Client.Models.Enums.AgentStatus)request.AgentStatus);

		var totalCount = directAgents.Count();

		var response =
			(directAgents.OrderByDescending(x => x.RegisteredOn).Chunk(request.Rows).Skip(request.Page - 1)
				.FirstOrDefault() ?? []).Select(x =>
			{
				var agentIds = Array.Empty<Guid>();

				GetAgentIds(x.AgentId, childAgentInfos, ref agentIds);

				agentIds = [.. agentIds, x.AgentId];

				var playerCount = agentPlayerCounts
					.Where(a => a.AgentId is not null)
					.Where(a => agentIds.Contains(a.AgentId.Value))
					.Sum(s => s.PlayerCount);

				return new DirectAgentsResponse
				{
					AgentDividendPercentage = x.DividendPercentage,
					AgentStatus = (AgentStatus)x.Status,
					AgentUsername = x.AgentUsername,
					RealName = x.RealName,
					RegisteredOn = x.RegisteredOn,
					DirectPlayerWithAgentCount = agentIds.Length + playerCount
				};
			})
			.ToArray();

		return new PagedResponse<DirectAgentsResponse>
		{
			Valid = true,
			TotalCount = totalCount,
			Items = response
		};
	}

	private static void GetAgentIds(Guid id, ChildAgentInfoModel[] models, ref Guid[] agentIds)
	{
		var childAgents = models.Where(x => x.ParentAgentId == id).ToArray();

		if (childAgents.Length <= 0)
			return;

		{
			agentIds = [.. agentIds, .. childAgents.Select(x => x.AgentId)];

			foreach (var childAgentInfoModel in childAgents)
			{
				GetAgentIds(childAgentInfoModel.AgentId, models, ref agentIds);
			}
		}
	}
}