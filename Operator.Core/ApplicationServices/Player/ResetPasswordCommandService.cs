using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Core.Infrastructures.Security;
using Tgs.ServiceUtility;
using TGS.Services.Operations;
using TGS.Services.Operator.ValidateCode;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class ResetPasswordCommandService(
	IPlayerRepository playerRepository,
	IPasswordHandler passwordHandler,
	IValidateCodeOperator validateCodeOperator,
	IPlayerOperations playerOperations) : ICommandService<ResetPasswordCommand>
{
	private readonly IPasswordHandler _passwordHandler = passwordHandler ?? throw new ArgumentNullException(nameof(passwordHandler));
	private readonly IPlayerRepository _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
	private readonly IPlayerOperations _playerOperations = playerOperations ?? throw new ArgumentNullException(nameof(playerOperations));

	private readonly IValidateCodeOperator _validateCodeOperator =
			validateCodeOperator ?? throw new ArgumentNullException(nameof(validateCodeOperator));

	public async ValueTask ExecuteAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default)
	{
		var result = await _validateCodeOperator
			.GetValidateCodeDataAsync<ValidateCodeData>(command.ValidateCode, command.SecurityKey)
			.ConfigureAwait(false)
			?? throw new ValidateCodeNotVerifyException();

		var password = _playerOperations.DecryptPassword(command.NewPassword);

		var (newSalt, newPassword) = _passwordHandler.GenerateCredential(password);

		await _playerRepository.UpdatePlayerLoginPasswordAsync(result.PlayerCode, newSalt, newPassword)
			.ConfigureAwait(false);
	}
}