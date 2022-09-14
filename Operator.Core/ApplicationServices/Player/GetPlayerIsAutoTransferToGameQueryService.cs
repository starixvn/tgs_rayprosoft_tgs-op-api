using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class GetPlayerIsAutoTransferToGameQueryService : IGetService<GetPlayerIsAutoTransferToGameQuery, bool>
{
    private readonly IPlayerRepository _playerRepository;

    public GetPlayerIsAutoTransferToGameQueryService(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
    }

    public ValueTask<bool> GetAsync(
        GetPlayerIsAutoTransferToGameQuery query,
        CancellationToken cancellationToken = default)
        => _playerRepository.GetPlayerAutoTransferFlagAsync(query.PlayerCode);
}
