using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using TGS.Services.Operations;
using TGS.Services.Operator.ValidateCode;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class CommitSetPlayerEmailCommandService(
	IValidateCodeOperator validateCodeOperator,
	IPlayerOperations playerOperations,
	IGetService<CheckBrandEmailQuery, bool> getService) : ICommandService<CommitSetPlayerEmailCommand>
{
	private readonly IGetService<CheckBrandEmailQuery, bool> _getService = getService ?? throw new ArgumentNullException(nameof(getService));
	private readonly IPlayerOperations _playerOperations = playerOperations ?? throw new ArgumentNullException(nameof(playerOperations));
	private readonly IValidateCodeOperator _validateCodeOperator = validateCodeOperator ?? throw new ArgumentNullException(nameof(validateCodeOperator));

	public async ValueTask ExecuteAsync(CommitSetPlayerEmailCommand command, CancellationToken cancellationToken = default)
	{
		var result = await _validateCodeOperator
			.GetValidateCodeDataAsync<EmailValidateData>(command.ValidateCode, command.SecurityKey)
			.ConfigureAwait(false)
			?? throw new ValidateCodeNotVerifyException();

		var updatedOn = DateTimeOffset.UtcNow;

		var isBrandHasSameEmail = await _getService
			.GetAsync(new CheckBrandEmailQuery(
				command.BrandId,
				result.Email,
				command.PlayerCode),
				cancellationToken)
			.ConfigureAwait(false);

		if (isBrandHasSameEmail)
			throw new BrandEmailDuplicateException();

		await _playerOperations
			.SetEmailAsync(command.PlayerCode, result.Email, updatedOn)
			.ConfigureAwait(false);
	}
}