using Microsoft.AspNetCore.Mvc;
using Tgs.Agent.Client.Models;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Agent;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Agent;
using TGS.Services.Operator.ApplicationServices.Brand;
using TGS.Services.Operator.ApplicationServices.Player;

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
		[FromServices] IGetService<GetPlatformChargeQuery, PlatformChargeInfo?> getPlatformChargeService,
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

		var platformChargeInfo =
			await getPlatformChargeService.GetAsync(new GetPlatformChargeQuery(brandId));

		var agentProductFeeInfo =
			await getAgentProductFeeInfoService.GetAsync(new GetAgentProductFeeInfoQuery(agentId.Value));

		var agentTeamInfo = await getAgentTeamInfoService.GetAsync(new GetAgentTeamInfoQuery(brandId, agentId.Value));

		return agentInfo is not null
			? new AgentInfoResponse
			{
				DividendPercentage = agentInfo.DividendPercentage,
				PlatformChargePercentage = platformChargeInfo?.PlatformChargePercentage ?? 0M,
				NegativeAgentDividend = negativeAgentDividend,
				CardGamePercentage = agentProductFeeInfo?.CardGamePercentage ?? 0M,
				LiveDealerPercentage = agentProductFeeInfo?.LiveDealerPercentage ?? 0M,
				LotteryPercentage = agentProductFeeInfo?.LotteryPercentage ?? 0M,
				SlotPercentage = agentProductFeeInfo?.SlotPercentage ?? 0M,
				SportsPercentage = agentProductFeeInfo?.SportsPercentage ?? 0M,
				VirtualSportsPercentage = agentProductFeeInfo?.VirtualSportsPercentage ?? 0M,
				DirectAgentCount = agentTeamInfo?.DirectAgentCount ?? 0,
				IndirectAgentCount = agentTeamInfo?.IndirectAgentCount ?? 0,
				DirectPlayerCount = agentTeamInfo?.DirectPlayerCount ?? 0,
				IndirectPlayerCount = agentTeamInfo?.IndirectPlayerCount ?? 0,
				InvitationCode = agentInfo.InvitationCode,
				NegativeAgentDividendCarryForward = agentInfo.NegativeAgentDividendCarryForward
			}
			: null;
	}
}