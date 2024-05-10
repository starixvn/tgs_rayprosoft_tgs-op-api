using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using TGS.Core.DataServices.Tgs;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Services.Operator;

namespace TGS.Operator.Repositories;

internal class PlayerAgentRepository(
	IPlayerServiceClient playerServiceClient) : IPlayerAgentRepository
{
	public async Task<Guid?> GetPlayerAgentIdAsync(string playerCode, CancellationToken cancellationToken)
	{
		return await playerServiceClient.GetAgentIdForPlayerUpgradedAsync(playerCode, cancellationToken);
	}
}