using System;
using System.Threading;
using System.Threading.Tasks;

namespace TGS.Services.Operator;

public interface IPlayerAgentRepository
{
	Task<Guid?> GetPlayerAgentIdAsync(string playerCode, CancellationToken cancellationToken);
}