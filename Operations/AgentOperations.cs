using System;
using System.Threading.Tasks;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Services.Operations.Commands;
using TGS.Services.Operations.Models.Services;
using TGS.Services.Operations.Queries;

namespace TGS.Services.Operations;

public sealed class AgentOperations : IAgentOperations
{
    private readonly IAgentCommands _agentCommands;
    private readonly IAgentQueries _agentQueries;

    public AgentOperations(IAgentQueries agentQueries, IAgentCommands agentCommands)
    {
        _agentQueries = agentQueries;
        _agentCommands = agentCommands;
    }

    public Task<AgentLoginRecord> AgentLoginAsync(AgentLoginData data) =>
        _agentCommands.AgentLoginAsync(data);

    public async Task ChangeAgentPasswordAsync(ChangeAgentPasswordData data)
    {
        await _agentCommands.ChangeAgentPasswordAsync(data);
    }

    public Task<Agent> GetAgentAsync(Guid agentId)
        => _agentQueries.GetAgentAsync(x => x.Id == agentId);

    public Task<Agent> GetAgentByInvitationCodeAsync(Guid brandId, string invitationCode)
        => _agentQueries.GetAgentByInvitationCodeAsync(brandId, invitationCode);

    public Task<AgentLoginRecord> GetAgentLoginRecordAsync(Guid loginRecordId) =>
        _agentQueries.GetAgentLoginRecordAsync(x => x.Id == loginRecordId);

    public Task SetEmailAsync(Guid agentId, string email, DateTimeOffset updatedOn)
    {
        return _agentCommands.SetEmailAsync(agentId, email, updatedOn);
    }
}
