using System;
using System.Threading.Tasks;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Services.Operations.Models.Services;

namespace TGS.Services.Operations;

public interface IAgentOperations
{
    Task<Agent> GetAgentAsync(Guid agentId);

    Task<AgentLoginRecord> GetAgentLoginRecordAsync(Guid loginRecordId);

    Task<Agent> GetAgentByInvitationCodeAsync(Guid brandId, string invitationCode);

    Task<AgentLoginRecord> AgentLoginAsync(AgentLoginData data);

    Task ChangeAgentPasswordAsync(ChangeAgentPasswordData data);

    Task SetEmailAsync(Guid agentId, string email, DateTimeOffset updatedOn);
}