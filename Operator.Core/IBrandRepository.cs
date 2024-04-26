#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TGS.Services.Operator;

public interface IBrandRepository
{
	ValueTask<string?> GetSecretKeyAsync(Guid brandId, CancellationToken cancellationToken = default);

	IAsyncEnumerable<AnnouncementInfo> QueryAnnouncementInfosAsync(Guid brandId, DateTimeOffset searchDate);

	IAsyncEnumerable<BannerInfo> QueryBrandBannersAsync(Guid brandId);
}