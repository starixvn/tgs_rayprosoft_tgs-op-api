using System;
using System.Collections.Generic;

namespace TGS.Services.Operator;

public interface IBrandRepository
{
    IAsyncEnumerable<AnnouncementInfo> QueryAnnouncementInfosAsync(Guid brandId, DateTimeOffset searchDate);

    IAsyncEnumerable<BannerInfo> QueryBrandBannersAsync(Guid brandId);
}
