using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using TGS.Core.DataServices.Tgs.Models;

namespace TGS.Services.Operations.Queries;

public interface IAgentQueries
{
    Task<Agent> GetAgentAsync(Expression<Func<Agent, bool>> predicate,
        Func<IQueryable<Agent>, IQueryable<Agent>> preQuery = null);

    Task<AgentLoginRecord> GetAgentLoginRecordAsync(Expression<Func<AgentLoginRecord, bool>> predicate,
        Func<IQueryable<AgentLoginRecord>, IQueryable<AgentLoginRecord>> preQuery = null);

    Task<Agent> GetAgentByInvitationCodeAsync(Guid brandId, string invitationCode);
}