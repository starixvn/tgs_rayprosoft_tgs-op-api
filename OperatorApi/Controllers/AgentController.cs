using Microsoft.AspNetCore.Mvc;
using Tgs.Agent.Client.Models;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Agent;
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

	[HttpGet("agent-info", Name = "GetInvitationAgentInfo")]
	public async Task<AgentInvitationInfoResponse?> GetAgentInfoAsync(
		[FromServices] IGetService<GetAgentInfoQuery, AgentInfoModel?> getService,
		[FromServices] IGetService<GetPlayerAgentQuery, Guid?> playerAgentService)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var agentId =
			await playerAgentService.GetAsync(new GetPlayerAgentQuery(playerTokenData!.PlayerCode), HttpContext.RequestAborted);

		var agentInfo = await getService.GetAsync(
			new GetAgentInfoQuery(agentId!.Value),
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

}