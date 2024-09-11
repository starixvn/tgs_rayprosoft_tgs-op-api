using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Tgs.ServiceUtility;
using TGS.Core.Models.Extensions;
using TGS.Services.Operator.GameProviders;

namespace TGS.Services.Operator.ApplicationServices.GameTypes;
public class GetGameTypesQueryService(IGameTypeRepository categoryRepository, IMemoryCache memoryCache) : IGetService<GetGameTypesQuery, IEnumerable<GameTypeModel>>
{
	private readonly IGameTypeRepository _categoryRepository =
		categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
	private readonly IMemoryCache _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

	public async ValueTask<IEnumerable<GameTypeModel>> GetAsync(GetGameTypesQuery query, CancellationToken cancellationToken = default)
	{
		var hashKey = HashCode.Combine(query.BrandId, query.PlayerType, query.PlatformType, query.CultureCode,
			query.PageSize, query.PageIndex, query.Types, query.Tag);
		var categories = await _memoryCache.GetOrCreateAsync($"GetGameType_{hashKey}", async entry =>
		{
			entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
			return await GetGameTypeAsync(query, cancellationToken);
		}).ConfigureAwait(false);

		return categories ?? new List<GameTypeModel>();
	}

	private async Task<IEnumerable<GameTypeModel>> GetGameTypeAsync(GetGameTypesQuery query, CancellationToken cancellationToken = default)
	{
		var gamePlatformTypes = query.PlatformType.ToGamePlatformTypes();

		var categories = _categoryRepository.QueryGameByGameTypesAsync(query, gamePlatformTypes);

		return await categories;
	}

}
