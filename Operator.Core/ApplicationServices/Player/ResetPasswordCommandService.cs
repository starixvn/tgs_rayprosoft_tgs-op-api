using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Core.Infrastructures.Security;
using Tgs.ServiceUtility;
using TGS.Services.Operations;
using TGS.Services.Operator.ValidateCode;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class ResetPasswordCommandService : ICommandService<ResetPasswordCommand>
{
    private readonly IPasswordHandler _passwordHandler;
    private readonly IPlayerOperations _playerOperations;
    private readonly IValidateCodeOperator _validateCodeOperator;

    public ResetPasswordCommandService(
        IPlayerOperations playerOperations,
        IPasswordHandler passwordHandler,
        IValidateCodeOperator validateCodeOperator)
    {
        _playerOperations = playerOperations ?? throw new ArgumentNullException(nameof(playerOperations));
        _passwordHandler = passwordHandler ?? throw new ArgumentNullException(nameof(passwordHandler));

        _validateCodeOperator =
            validateCodeOperator ?? throw new ArgumentNullException(nameof(validateCodeOperator));
    }

    public async ValueTask ExecuteAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        var result = await _validateCodeOperator
            .GetValidateCodeDataAsync<ValidateCodeData>(command.ValidateCode, command.SecurityKey)
            .ConfigureAwait(false);

        if (result == null)
            throw new ValidateCodeNotVerifyException();

        var username = await _playerOperations
            .GetUsernameByPlayerCodeAsync(result.PlayerCode)
            .ConfigureAwait(false);
        var updatedOn = DateTimeOffset.UtcNow;

        var (newSalt, newPassword) = _passwordHandler.GenerateCredential(command.NewPassword);

        await _playerOperations
            .ChangePlayerPasswordAsync(new()
            {
                Username = username,
                PlayerCode = result.PlayerCode,
                Salt = newSalt,
                Password = newPassword,
                UpdatedOn = updatedOn
            })
            .ConfigureAwait(false);
    }
}
