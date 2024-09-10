using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Tgs.ServiceUtility;
using TGS.Core.Models.Extensions;
using TGS.Services.Operator.GameProviders;

namespace TGS.Services.Operator.ApplicationServices.Category;
public class GetCategoryQueryService(ICategoryRepository categoryRepository, IMemoryCache memoryCache) : IGetService<GetCategoriesQuery, IEnumerable<Operator.Category>>
{
	private readonly ICategoryRepository _categoryRepository =
		categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
	private readonly IMemoryCache _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

	public async ValueTask<IEnumerable<Operator.Category>> GetAsync(GetCategoriesQuery query, CancellationToken cancellationToken = default)
	{
		var hashKey = HashCode.Combine(query.BrandId, query.PlayerType, query.PlatformType, query.CultureCode,
			query.PageSize, query.PageIndex, query.Types, query.Tag);
		var categories = await _memoryCache.GetOrCreateAsync($"GetCategory_{hashKey}", async entry =>
		{
			entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
			return await GetCategoryAsync(query, cancellationToken);
		}).ConfigureAwait(false);

		return categories ?? new List<Operator.Category>();
	}

	private async Task<IEnumerable<Operator.Category>> GetCategoryAsync(GetCategoriesQuery query, CancellationToken cancellationToken = default)
	{
		var gamePlatformTypes = query.PlatformType.ToGamePlatformTypes();

		var categories = _categoryRepository.QueryGameByGameTypesAsync(query, gamePlatformTypes);

		return await categories;
	}

}
