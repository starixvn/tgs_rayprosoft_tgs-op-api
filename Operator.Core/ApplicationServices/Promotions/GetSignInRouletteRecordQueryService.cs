using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.RouletteServices.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Promotions;

internal class GetSignInRouletteRecordQueryService : IGetService<GetSignInRouletteRecordQuery, RouletteResult?>
{
    private readonly IRouletteSignInClient _rouletteSignInClient;

    public GetSignInRouletteRecordQueryService(IRouletteSignInClient rouletteSignInClient)
    {
        _rouletteSignInClient = rouletteSignInClient ?? throw new ArgumentNullException(nameof(rouletteSignInClient));
    }

    public async ValueTask<RouletteResult?> GetAsync(GetSignInRouletteRecordQuery query, CancellationToken cancellationToken = default)
    {
        var currentDate = DateOnly.FromDateTime(DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(8)).DateTime);

        var record = await _rouletteSignInClient.GetRouletteRecordAsync(
            query.PromotionId,
            query.PlayerCode,
            currentDate,
            cancellationToken).ConfigureAwait(false);

        return record is null
            ? null
            : new RouletteResult(record.Value.Id, record.Value.ItemIndex);
    }
}
