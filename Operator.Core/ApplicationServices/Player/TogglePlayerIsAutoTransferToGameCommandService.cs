using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class TogglePlayerIsAutoTransferToGameCommandService : ICommandService<TogglePlayerIsAutoTransferToGameCommand>
{
    private readonly IPlayerRepository _playerRepository;

    public TogglePlayerIsAutoTransferToGameCommandService(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
    }

    public ValueTask ExecuteAsync(
        TogglePlayerIsAutoTransferToGameCommand command,
        CancellationToken cancellationToken = default)
        => _playerRepository.TogglePlayerAutoTransferFlagAsync(command.PlayerCode);
}
