using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Core.Infrastructures.Security;
using Tgs.ServiceUtility;
using TGS.Services.Operations;
using TGS.Services.Operator.ValidateCode;

namespace TGS.Services.Operator.ApplicationServices.PayPassowrd;

internal class ChangePayPasswordCommandService : ICommandService<ChangePayPasswordCommand>
{
	private readonly IPasswordHandler _passwordHandler;
	private readonly IPlayerOperations _playerOperations;
	private readonly IValidateCodeOperator _validateCodeOperator;

	public ChangePayPasswordCommandService(
		IPlayerOperations playerOperations,
		IPasswordHandler passwordHandler,
		IValidateCodeOperator validateCodeOperator)
	{
		_playerOperations = playerOperations ?? throw new ArgumentNullException(nameof(playerOperations));
		_passwordHandler = passwordHandler ?? throw new ArgumentNullException(nameof(passwordHandler));

		_validateCodeOperator =
			validateCodeOperator ?? throw new ArgumentNullException(nameof(validateCodeOperator));
	}

	public async ValueTask ExecuteAsync(
		ChangePayPasswordCommand command, CancellationToken cancellationToken = default)
	{
		_ = await _validateCodeOperator
			.GetValidateCodeDataAsync<ValidateCodeData>(command.ValidateCode, command.SecurityKey)
			.ConfigureAwait(false)
			?? throw new ValidateCodeNotVerifyException();

		var updatedOn = DateTimeOffset.UtcNow;

		var (newSalt, newPassword) = _passwordHandler.GenerateCredential(command.NewPassword);

		await _playerOperations.SetPayPasswordAsync(
			command.PlayerCode,
			newPassword,
			newSalt,
			updatedOn).ConfigureAwait(false);
	}
}
