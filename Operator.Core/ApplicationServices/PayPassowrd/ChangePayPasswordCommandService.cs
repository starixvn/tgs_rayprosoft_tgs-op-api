using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Core.Infrastructures.Security;
using Tgs.ServiceUtility;
using TGS.Services.Operations;
using TGS.Services.Operator.ValidateCode;

namespace TGS.Services.Operator.ApplicationServices.PayPassowrd;

internal class ChangePayPasswordCommandService(
	IPlayerOperations playerOperations,
	IPasswordHandler passwordHandler,
	IValidateCodeOperator validateCodeOperator) : ICommandService<ChangePayPasswordCommand>
{
	private readonly IPasswordHandler _passwordHandler = passwordHandler ?? throw new ArgumentNullException(nameof(passwordHandler));
	private readonly IPlayerOperations _playerOperations = playerOperations ?? throw new ArgumentNullException(nameof(playerOperations));

	private readonly IValidateCodeOperator _validateCodeOperator =
			validateCodeOperator ?? throw new ArgumentNullException(nameof(validateCodeOperator));

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