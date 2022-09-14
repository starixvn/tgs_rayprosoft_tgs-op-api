using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Core.Infrastructures.Security;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

public class PlayerPasswordValidateCommandService : IGetService<PlayerPasswordValidateCommand, bool>
{
    private readonly IPasswordHandler _passwordHandler;
    private readonly IPlayerRepository _playerRepository;

    public PlayerPasswordValidateCommandService(
        IPlayerRepository playerRepository,
        IPasswordHandler passwordHandler)
    {
        _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
        _passwordHandler = passwordHandler ?? throw new ArgumentNullException(nameof(passwordHandler));
    }

    public async ValueTask<bool> GetAsync(
        PlayerPasswordValidateCommand command,
        CancellationToken cancellationToken = default)
    {
        var passwordInfo = await _playerRepository
            .GetPlayerLoginPasswordInfoAsync(command.PlayerCode)
            .ConfigureAwait(false);

        return _passwordHandler.Verify(
            passwordInfo.Password,
            passwordInfo.Salt,
            command.Password);
    }
}
