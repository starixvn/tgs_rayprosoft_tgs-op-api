using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

public class GetPlayerInfoByUserNameQueryService : IGetService<GetPlayerInfoByUserNameQuery, Tgs.PlayerService.Client.PlayerInfo?>
{
    private readonly IPlayerServiceClient _playerServiceClient;

    public GetPlayerInfoByUserNameQueryService(
        IPlayerServiceClient playerServiceClient)
    {
        _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
    }

    public ValueTask<Tgs.PlayerService.Client.PlayerInfo?> GetAsync(
        GetPlayerInfoByUserNameQuery query,
        CancellationToken cancellationToken = default)
        => _playerServiceClient.GetPlayerInfoByUserNameAsync(
            query.BrandId,
            query.UserName,
            cancellationToken);
}
