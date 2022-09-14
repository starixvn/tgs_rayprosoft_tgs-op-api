using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TGS.Core.DataServices.Tgs;
using TGS.Services.Operator;

namespace TGS.Operator.Repositories;

internal class AgentRepository : IAgentRepository
{
    private readonly ITgsRepository _tgsRepository;

    public AgentRepository(ITgsRepository tgsRepository)
    {
        _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
    }
    public ValueTask<AgentInfo?> GetAgentInfoByInvitationCodeAsync(string invitationCode)
    {
        return _tgsRepository.Agents.AsQueryable()
            .Where(agent => agent.InvitationCode == invitationCode)
            .Select(agent => new AgentInfo(agent.Id, agent.Username, agent.InvitationCode))
            .ToAsyncEnumerable()
            .FirstOrDefaultAsync();
    }
}
