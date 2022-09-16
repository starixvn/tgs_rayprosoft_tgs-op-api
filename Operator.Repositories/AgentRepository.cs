using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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

    public async ValueTask<AgentInfo?> GetAgentInfoByInvitationCodeAsync(
        string invitationCode,
        CancellationToken cancellationToken = default)
        => await _tgsRepository.Agents.AsQueryable()
            .Where(agent => agent.InvitationCode == invitationCode)
            .Select(agent => new AgentInfo(agent.Id, agent.Username, agent.InvitationCode, agent.Status))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
}
