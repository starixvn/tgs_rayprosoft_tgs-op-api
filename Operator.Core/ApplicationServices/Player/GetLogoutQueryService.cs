using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Core.Models;
using TGS.Services.Operations.Validators;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class GetLogoutQueryService : IGetService<GetLogoutQuery, LogoutInfo>
{
    private readonly IPlayerLoginRecordRepository _loginRecordRepository;
    private readonly IPlayerServiceClient _playerServiceClient;
    private readonly IStatusValidator _statusValidator;

    public GetLogoutQueryService(
        IStatusValidator statusValidator,
        IPlayerServiceClient playerServiceClient,
        IPlayerLoginRecordRepository loginRecordRepository)
    {
        _statusValidator = statusValidator ?? throw new ArgumentNullException(nameof(statusValidator));
        _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
        _loginRecordRepository = loginRecordRepository ?? throw new ArgumentNullException(nameof(loginRecordRepository));
    }

    public async ValueTask<LogoutInfo> GetAsync(GetLogoutQuery query, CancellationToken cancellationToken = default)
    {
        await _statusValidator.ThrowIfBrandStatusIsNotOneOfAsync(query.BrandId, ValidStatus.Default);

        var player = await _playerServiceClient
            .GetPlayerInfoAsync(query.PlayerCode, cancellationToken)
            .ConfigureAwait(false);

        if (player == null)
            throw new PlayerNotFoundException("");

        await _loginRecordRepository
            .DeauthorizePlayerAsync(query.LoginRecordId)
            .ConfigureAwait(false);

        return new LogoutInfo
        {
            UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }
}
