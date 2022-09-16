using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Agent;

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

        if (agentInfo is not null)
            return new AgentInvitationInfoResponse
            {
                AgentId = agentInfo.Id,
                UserName = agentInfo.UserName,
                InvitationCode = agentInfo.InvitationCode
            };
        else
            return null;
    }
}
