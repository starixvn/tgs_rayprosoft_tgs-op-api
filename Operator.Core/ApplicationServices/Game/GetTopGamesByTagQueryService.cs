using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Tgs.ServiceUtility;
using TGS.Core.Models.Extensions;
using TGS.Services.Operator.GameProviders;

namespace TGS.Services.Operator.ApplicationServices.Game;
public class GetTopGamesByTagQueryService(IGameProviderRepository gameProviderRepository, IMemoryCache memoryCache) : IGetService<GetTopGamesByTagQuery, IEnumerable<GameTagInfo>>
{
	private readonly IGameProviderRepository _gameProviderRepository = gameProviderRepository ?? throw new ArgumentNullException(nameof(gameProviderRepository));

	private readonly IMemoryCache _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

	public async ValueTask<IEnumerable<GameTagInfo>> GetAsync(GetTopGamesByTagQuery query, CancellationToken cancellationToken = default)
	{

		//memoryCache
		//var hashKey = HashCode.Combine(query.BrandId, query.PlatformType, query.CultureCode, query.Limit, query.Tags);
		//var gameTagInfos = await _memoryCache.GetOrCreateAsync($"GetGameTag_{hashKey}", async entry =>
		//{
		//	entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
		//	return await GetTopGameByTagAsync(query, cancellationToken);
		//}).ConfigureAwait(false);

		//	return gameTagInfos ?? new List<GameTagInfo>();

		return await GetTopGameByTagAsync(query, cancellationToken);
	}

	private async Task<IEnumerable<GameTagInfo>> GetTopGameByTagAsync(GetTopGamesByTagQuery query, CancellationToken cancellationToken = default)
	{
		var gamePlatformTypes = query.PlatformType.ToGamePlatformTypes();

		var gamesByTag = await _gameProviderRepository.QueryGameByTagsAsync(query.BrandId, query.Tags, gamePlatformTypes, query.Limit);

		return gamesByTag;
	}
}
