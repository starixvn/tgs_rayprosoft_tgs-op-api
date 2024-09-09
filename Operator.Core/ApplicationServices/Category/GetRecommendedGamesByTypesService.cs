using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Tgs.ServiceUtility;
using TGS.Core.Models.Extensions;
using TGS.Services.Operator.GameProviders;

namespace TGS.Services.Operator.ApplicationServices.Category;
public class GetRecommendedGamesByTypesService(ICategoryRepository categoryRepository, IMemoryCache memoryCache) : IGetService<GetRecommendedGamesByTypesQuery, IEnumerable<Operator.Category>>
{
	private readonly ICategoryRepository _categoryRepository =
		categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));

	private readonly IMemoryCache _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

	public async ValueTask<IEnumerable<Operator.Category>> GetAsync(GetRecommendedGamesByTypesQuery query, CancellationToken cancellationToken = default)
	{
		var categories = await _memoryCache.GetOrCreateAsync($"GetRecommendedGames_{query.BrandId}_{query.PlatformType}_{string.Join(',', query.Types)}_{query.Limit}", async entry =>
		{
			entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
			return await GetCategoryAsync(query, cancellationToken);
		}).ConfigureAwait(false);

		return categories ?? new List<Operator.Category>();
	}

	private async Task<IEnumerable<Operator.Category>> GetCategoryAsync(GetRecommendedGamesByTypesQuery query, CancellationToken cancellationToken = default)
	{
		var gamePlatformTypes = query.PlatformType.ToGamePlatformTypes();

		var categories = _categoryRepository.QueryRecommendedGamesByTypesAsync(query, gamePlatformTypes);

		return await categories;
	}

}

