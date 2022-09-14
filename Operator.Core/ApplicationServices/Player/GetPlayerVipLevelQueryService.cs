using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class GetPlayerVipLevelQueryService : IGetService<GetPlayerVipLevelQuery, int>
{
    private readonly IVipLevelRepository _vipLevelRepository;

    public GetPlayerVipLevelQueryService(
        IVipLevelRepository vipLevelRepository)
    {
        _vipLevelRepository = vipLevelRepository ?? throw new ArgumentNullException(nameof(vipLevelRepository));
    }

    public async ValueTask<int> GetAsync(GetPlayerVipLevelQuery query, CancellationToken cancellationToken = default)
    {
        int? vipLevel = null;

        if (query.VipLevelId.HasValue)
        {
            vipLevel = await _vipLevelRepository.GetVipLevelAsync(query.VipLevelId.Value)
                .ConfigureAwait(false);
        }

        return vipLevel ?? 0;
    }
}
