using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.RouletteServices.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Promotions;

internal class SignInRouletteCommandService : IGetService<SignInRouletteCommand, RouletteResult>
{
    private readonly IRouletteSignInClient _rouletteSignInClient;

    public SignInRouletteCommandService(IRouletteSignInClient rouletteSignInClient)
    {
        _rouletteSignInClient = rouletteSignInClient ?? throw new ArgumentNullException(nameof(rouletteSignInClient));
    }

    public async ValueTask<RouletteResult> GetAsync(SignInRouletteCommand command, CancellationToken cancellationToken = default)
    {
        var currentDate = DateOnly.FromDateTime(DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(8)).DateTime);
        var record = await _rouletteSignInClient
            .RouletteAsync(
                command.PromotionId,
                command.PlayerCode,
                currentDate,
                cancellationToken)
            .ConfigureAwait(false);

        return new RouletteResult(record.Id, record.ItemIndex);
    }
}
