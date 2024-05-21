using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace TGS.Services.Operator;

internal class MemoryCachedPlayerAgentRepository(
	IMemoryCache memoryCache,
	IPlayerAgentRepository playerAgentRepository) : IPlayerAgentRepository
{
	private readonly IMemoryCache _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

	public async Task<Guid?> GetPlayerAgentIdAsync(string playerCode,
		CancellationToken cancellationToken)
	{
		var key = $"PlayerAgentId:{playerCode}";

		var agentId = _memoryCache.Get<Guid?>(key);

		if (agentId is not null)
		{
			return agentId;
		}

		var playerAgentId = await playerAgentRepository.GetPlayerAgentIdAsync(playerCode, cancellationToken);

		if (playerAgentId is not null)
		{
			_ = _memoryCache.Set(key, playerAgentId, TimeSpan.FromDays(1D));
		}

		return playerAgentId;
	}
}