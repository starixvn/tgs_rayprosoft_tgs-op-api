using System;
using System.Collections.Generic;
using System.Threading;
using Tgs.ServiceUtility;
using Tgs.Paging;

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
    {
        if (query.Paging.ShowAll)
            return _vipLevelRepository.QueryVipLevelsAsync(query.BrandId);

        var paging = new Paging<VipRecord>
        {
            Page = query.Paging.Page,
            RowCount = query.Paging.Rows
        };

        paging.SortByPropertyName(query.Paging.SortBy, query.Paging.IsDesc);

        return _vipLevelRepository.QueryVipLevelsAsync(query.BrandId, paging);
    }
}
