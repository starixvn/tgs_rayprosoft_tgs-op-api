using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using TGS.Services.Operations;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class SetIdentityCommandService : ICommandService<SetIdentityCommand>
{
    private readonly IPlayerOperations _playerOperations;

    public SetIdentityCommandService(IPlayerOperations playerOperations)
    {
        _playerOperations = playerOperations ?? throw new ArgumentNullException(nameof(playerOperations));
    }

    public async ValueTask ExecuteAsync(SetIdentityCommand command, CancellationToken cancellationToken = default)
    {
        var player = await _playerOperations
            .GetPlayerByCodeAsync(command.PlayerCode)
            .ConfigureAwait(false);

        var updatedOn = DateTimeOffset.UtcNow;
        await _playerOperations
            .SetIdentityAsync(
                command.PlayerCode,
                command.RealName,
                command.Identity,
                false,
                updatedOn)
            .ConfigureAwait(false);
    }
}
