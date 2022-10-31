using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Banner;

internal class GetBannersQueryService : IQueryService<GetBannersQuery, BannerInfo>
{
	private readonly IBrandRepository _sysRepository;

	public GetBannersQueryService(IBrandRepository brandRepository)
	{
		_sysRepository = brandRepository ?? throw new ArgumentNullException(nameof(brandRepository));
	}

	public IAsyncEnumerable<BannerInfo> QueryAsync(GetBannersQuery query, CancellationToken cancellationToken = default)
		=> _sysRepository.QueryBrandBannersAsync(query.BrandId).Take(10);
}
