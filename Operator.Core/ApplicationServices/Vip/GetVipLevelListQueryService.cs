using System;
using System.Collections.Generic;
using System.Threading;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Vip;

internal class GetVipLevelListQueryService(IVipLevelRepository vipLevelRepository) : IQueryService<GetVipLevelListQuery, VipRecord>
{
	private readonly IVipLevelRepository _vipLevelRepository = vipLevelRepository ??
			throw new ArgumentNullException(nameof(vipLevelRepository));

	public IAsyncEnumerable<VipRecord> QueryAsync(
		GetVipLevelListQuery query,
		CancellationToken cancellationToken = default)
		=> _vipLevelRepository.QueryVipLevelsAsync(query.BrandId, query.Paging);
}