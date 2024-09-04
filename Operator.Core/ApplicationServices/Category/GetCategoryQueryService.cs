using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Tgs.ServiceUtility;
using TGS.Core.Models.Extensions;
using TGS.Services.Operator.GameProviders;

namespace TGS.Services.Operator.ApplicationServices.Category;
public class GetCategoryQueryService(IGameProviderRepository gameProviderRepository, IMemoryCache memoryCache) : IGetService<GetCategoriesQuery, IEnumerable<Operator.Category>>
{
	private readonly IGameProviderRepository _gameProviderRepository =
		gameProviderRepository ?? throw new ArgumentNullException(nameof(gameProviderRepository));

	private readonly IMemoryCache _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

	public async ValueTask<IEnumerable<Operator.Category>> GetAsync(GetCategoriesQuery query, CancellationToken cancellationToken = default)
	{
		var categories = await _memoryCache.GetOrCreateAsync($"GetCategories_{query.BrandId}_{query.PlatformType}_{query.Type}_{query.PageSize}_{query.PageIndex}", async entry =>
		{
			entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
			return await GetCategory(query, cancellationToken);
		}).ConfigureAwait(false);

		return categories ?? new List<Operator.Category>();
	}

	private async Task<IEnumerable<Operator.Category>> GetCategory(GetCategoriesQuery query, CancellationToken cancellationToken = default)
	{
		var gamePlatformTypes = query.PlatformType.ToGamePlatformTypes();

		var games = _gameProviderRepository.QueryGameByGameTypeAsync(query.BrandId, gamePlatformTypes, query.Type);
		var categories = games.GroupBy(i => i.Type).Select(async g =>
		{
			var totalGames = await _gameProviderRepository.TotalGameByGameTypeAsync(query.BrandId, gamePlatformTypes, g.Key).ConfigureAwait(false);
			return new Operator.Category
			{
				PageSize = query.PageSize,
				PageIndex = query.PageIndex,
				Total = totalGames,
				CategoryName = g.Key.ToString(),
				Games = g
					.OrderBy(g => g.DisplayOrder)
					.ThenBy(g => g.Code)
					.Skip(query.PageSize * (query.PageIndex - 1))
					.Take(query.PageSize)
					.ToEnumerable()
			};
		});

		return await Task.WhenAll(categories.ToEnumerable());
	}

}
