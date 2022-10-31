using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Core.Infrastructures.Security;
using Tgs.ServiceUtility;
using TGS.Services.Operations;

namespace TGS.Services.Operator.ApplicationServices.PayPassowrd;

internal class SetFirstPayPasswordCommandService : ICommandService<SetFirstPayPasswordCommand>
{
	private readonly IPasswordHandler _passwordHandler;
	private readonly IPlayerOperations _playerOperations;

	public SetFirstPayPasswordCommandService(IPlayerOperations playerOperations, IPasswordHandler passwordHandler)
	{
		_playerOperations = playerOperations ?? throw new ArgumentNullException(nameof(playerOperations));
		_passwordHandler = passwordHandler;
	}

	public async ValueTask ExecuteAsync(
		SetFirstPayPasswordCommand command, CancellationToken cancellationToken = default)
	{
		var player = await _playerOperations
			.GetPlayerByCodeAsync(command.PlayerCode)
			.ConfigureAwait(false);
		var updatedOn = DateTimeOffset.UtcNow;

		if (player.WithdrawPassword.Length > 0)
			throw new PayPasswordAlreadySetException();

		var (newSalt, newPassword) = _passwordHandler.GenerateCredential(command.Password);
		await _playerOperations.SetPayPasswordAsync(command.PlayerCode, newPassword, newSalt, updatedOn);
	}
}
