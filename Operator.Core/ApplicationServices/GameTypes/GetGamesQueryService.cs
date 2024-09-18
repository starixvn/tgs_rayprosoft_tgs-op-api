using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Tgs.ServiceUtility;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Extensions;
using TGS.Services.Operator.ApplicationServices.MemoryCache;
using TGS.Services.Operator.GameProviders;

namespace TGS.Services.Operator.ApplicationServices.GameTypes;
public class GetGamesQueryService(IGameTypeRepository categoryRepository, IMemoryCacheService memoryCache) : IGetService<GetGamesQuery, IEnumerable<GamesModel>>
{
	private readonly IGameTypeRepository _categoryRepository =
		categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
	private readonly IMemoryCacheService _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

	public async ValueTask<IEnumerable<GamesModel>> GetAsync(GetGamesQuery query, CancellationToken cancellationToken = default)
	{
		var hashKey = HashCode.Combine(
											query.BrandId,
											query.PlatformType,
											query.PageSize,
											query.PageIndex,
											string.Join(',', query.Types.IsNullOrEmpty() ? Array.Empty<GameType>() : query.Types),
											string.Join(',', query.Tag.IsNullOrEmpty() ? Array.Empty<GameTag>() : query.Tag),
											query.OrderType,
											string.Join(",", query.ProviderCode.IsNullOrEmpty() ? Array.Empty<string>() : query.ProviderCode));
		var categories = await _memoryCache.GetOrCreateAsync(
			$"{query.BrandId}_GetGameType_{hashKey}",
			() => GetGameTypeAsync(query, cancellationToken),
			new List<GamesModel>());

		return categories;
	}

	private async Task<IEnumerable<GamesModel>> GetGameTypeAsync(GetGamesQuery query, CancellationToken cancellationToken = default)
	{
		var gamePlatformTypes = query.PlatformType.ToGamePlatformTypes();

		var categories = _categoryRepository.QueryGameByGameTypesAsync(query, gamePlatformTypes);

		return await categories;
	}

}
