using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Core.Infrastructures.Security;
using Tgs.ServiceUtility;
using TGS.Services.Operator.ValidateCode;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class ResetPasswordCommandService(
	IPlayerRepository playerRepository,
	IPasswordHandler passwordHandler,
	IValidateCodeOperator validateCodeOperator) : ICommandService<ResetPasswordCommand>
{
	private readonly IPasswordHandler _passwordHandler = passwordHandler ?? throw new ArgumentNullException(nameof(passwordHandler));
	private readonly IPlayerRepository _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));

	private readonly IValidateCodeOperator _validateCodeOperator =
			validateCodeOperator ?? throw new ArgumentNullException(nameof(validateCodeOperator));

	public async ValueTask ExecuteAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default)
	{
		var result = await _validateCodeOperator
			.GetValidateCodeDataAsync<ValidateCodeData>(command.ValidateCode, command.SecurityKey)
			.ConfigureAwait(false)
			?? throw new ValidateCodeNotVerifyException();

		var (newSalt, newPassword) = _passwordHandler.GenerateCredential(command.NewPassword);

		await _playerRepository.UpdatePlayerLoginPasswordAsync(result.PlayerCode, newSalt, newPassword)
			.ConfigureAwait(false);
	}
}