using System;
using System.Collections.Generic;
using System.Threading;
using Tgs.Paging;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Vip;

internal class GetVipLevelListQueryService : IQueryService<GetVipLevelListQuery, VipRecord>
{
	private readonly IVipLevelRepository _vipLevelRepository;

	public GetVipLevelListQueryService(IVipLevelRepository vipLevelRepository)
	{
		_vipLevelRepository = vipLevelRepository ??
			throw new ArgumentNullException(nameof(vipLevelRepository));
	}

	public IAsyncEnumerable<VipRecord> QueryAsync(
		GetVipLevelListQuery query,
		CancellationToken cancellationToken = default)
		=> _vipLevelRepository.QueryVipLevelsAsync(query.BrandId, query.Paging);
}
