using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Tgs.ServiceUtility;
using TGS.Core.Models.Extensions;
using TGS.Services.Operator.ApplicationServices.MemoryCache;
using TGS.Services.Operator.GameProviders;

namespace TGS.Services.Operator.ApplicationServices.Game;
public class GetTopGamesByTagQueryService(IGameProviderRepository gameProviderRepository, IMemoryCacheService memoryCache) : IGetService<GetTopGamesByTagQuery, IEnumerable<GameTagInfo>>
{
	private readonly IGameProviderRepository _gameProviderRepository = gameProviderRepository ?? throw new ArgumentNullException(nameof(gameProviderRepository));

	private readonly IMemoryCacheService _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

	public async ValueTask<IEnumerable<GameTagInfo>> GetAsync(GetTopGamesByTagQuery query, CancellationToken cancellationToken = default)
	{
		var hashKey = HashCode.Combine(
										query.BrandId,
										query.PlatformType,
										query.CultureCode,
										query.PageSize,
										query.PageIndex,
										string.Join(",", query.Tags),
										string.Join(",", query.ProviderCode.IsNullOrEmpty() ? Array.Empty<string>() : query.ProviderCode));
		var gameTagInfos = await _memoryCache.GetOrCreateAsync($"" +
			$"{query.BrandId}_GetGameTag_{hashKey}",
			() => GetTopGameByTagAsync(query, cancellationToken), new List<GameTagInfo>());

		return gameTagInfos;
	}

	private async Task<IEnumerable<GameTagInfo>> GetTopGameByTagAsync(GetTopGamesByTagQuery query, CancellationToken cancellationToken = default)
	{
		var gamePlatformTypes = query.PlatformType.ToGamePlatformTypes();

		var gamesByTag = await _gameProviderRepository.QueryGameByTagsAsync(query, gamePlatformTypes);

		return gamesByTag;
	}
}
