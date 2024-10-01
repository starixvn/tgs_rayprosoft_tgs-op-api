using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Core.Infrastructures.Security;
using Tgs.ServiceUtility;
using TGS.Core.Models.Exceptions;
using TGS.Services.Operations;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class ChangePasswordQueryService(
	IPlayerRepository playerRepository,
	IPlayerOperations playerOperations,
	IPasswordHandler passwordHandler) : IGetService<ChangePasswordQuery, ChangePasswordInfo>
{
	private readonly IPasswordHandler _passwordHandler = passwordHandler ?? throw new ArgumentNullException(nameof(passwordHandler));
	private readonly IPlayerRepository _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
	private readonly IPlayerOperations _playerOperations = playerOperations ?? throw new ArgumentNullException(nameof(playerOperations));

	public async ValueTask<ChangePasswordInfo> GetAsync(
		ChangePasswordQuery query, CancellationToken cancellationToken = default)
	{
		var passwordInfo = await _playerRepository
			.GetPlayerLoginPasswordInfoAsync(query.PlayerCode)
			.ConfigureAwait(false);

		var decryptedOldPassword = _playerOperations.DecryptPassword(query.OldPassword);
		var decryptedNewPassword = _playerOperations.DecryptPassword(query.NewPassword);

		if (!_passwordHandler.Verify(passwordInfo.Password, passwordInfo.Salt, decryptedOldPassword))
		{
			throw new InvalidCredentialsException("");
		}

		var (newSalt, newPassword) = _passwordHandler.GenerateCredential(decryptedNewPassword);

		await _playerRepository
			.UpdatePlayerLoginPasswordAsync(query.PlayerCode, newSalt, newPassword)
			.ConfigureAwait(false);

		return new ChangePasswordInfo
		{
			PlayerCode = query.PlayerCode,
			UpdateTime = DateTimeOffset.UtcNow
		};
	}
}