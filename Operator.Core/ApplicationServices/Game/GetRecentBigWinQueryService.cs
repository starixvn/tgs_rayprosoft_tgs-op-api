using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Tgs.ServiceUtility;
using TGS.Core.DataServices.Tgs;
using TGS.Services.Operator.ApplicationServices.MemoryCache;
using TGS.Services.Operator.GameProviders;

namespace TGS.Services.Operator.ApplicationServices.Game;
public class GetRecentBigWinQueryService(IGamesRepository categoryRepository, IMemoryCacheService memoryCache, ITgsRepository repository)
	: IGetService<GetRecentBigWinQuery, IEnumerable<RecentBigWinModel>>
{
	private readonly IGamesRepository _categoryRepository =
		categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
	private readonly ITgsRepository _repository =
		repository ?? throw new ArgumentNullException(nameof(repository));
	private readonly IMemoryCacheService _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

	public async ValueTask<IEnumerable<RecentBigWinModel>> GetAsync(GetRecentBigWinQuery query, CancellationToken cancellationToken = default)
	{
		var hashKey = HashCode.Combine(query.BrandId, query.Top);
		var categories = await _memoryCache.GetOrCreateAsync(
			$"{query.BrandId}_GetGameType_{hashKey}",
			() => GetRecentBigWinsAsync(query, cancellationToken),
			new List<RecentBigWinModel>());

		return categories;
	}

	private async Task<IEnumerable<RecentBigWinModel>> GetRecentBigWinsAsync(GetRecentBigWinQuery query, CancellationToken cancellationToken = default)
	{
		var db = _repository.GetDbConnection();
		var parameters = new Dictionary<string, object>
		{
			{ "brandid", query.BrandId },
			{ "top", query.Top }
		};
		string sqlQuery = @"
                SELECT * FROM dbo.recent_big_win_get(
                    @brandid,
                    @top
                )";
		var results = await db.QueryAsync<RecentBigWinModel>(sqlQuery, parameters);
		return results;

	}

}
