using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tgs.ServiceUtility;
using TGS.Core.DataServices.Tgs;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

internal class GetRequireAmountQueryService : IGetService<GetRequireAmountQuery, decimal>
{
    private readonly ITgsRepository _tgsRepository;

    public GetRequireAmountQueryService(ITgsRepository tgsRepository)
    {
        _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
    }

    public async ValueTask<decimal> GetAsync(GetRequireAmountQuery query, CancellationToken cancellationToken = default)
    {
        var playerBuckets = await _tgsRepository.PlayerBuckets
            .AsQueryable()
            .Where(x => x.PlayerCode == query.PlayerCode)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return playerBuckets.Sum(p => p.BucketAmount) - playerBuckets.Sum(p => p.CurrentBucketAmount);
    }
}
