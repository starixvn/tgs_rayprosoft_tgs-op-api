using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Vip;

internal class CountVipLevelsQueryService(IVipLevelRepository vipLevelRepository) : IGetService<GetVipLevelListQuery, int>
{
	private readonly IVipLevelRepository _vipLevelRepository = vipLevelRepository ??
			throw new ArgumentNullException(nameof(vipLevelRepository));

	public async ValueTask<int> GetAsync(
		GetVipLevelListQuery query,
		CancellationToken cancellationToken = default)
		=> await _vipLevelRepository
			.QueryVipLevelsTotalCountAsync(query.BrandId)
			.ConfigureAwait(false);
}