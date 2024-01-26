using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Core.Infrastructures.Security;
using Tgs.ServiceUtility;
using TGS.Core.Models.Exceptions;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class ChangePasswordQueryService(
	IPlayerRepository playerRepository,
	IPasswordHandler passwordHandler) : IGetService<ChangePasswordQuery, ChangePasswordInfo>
{
	private readonly IPasswordHandler _passwordHandler = passwordHandler ?? throw new ArgumentNullException(nameof(passwordHandler));
	private readonly IPlayerRepository _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));

	public async ValueTask<ChangePasswordInfo> GetAsync(
		ChangePasswordQuery query, CancellationToken cancellationToken = default)
	{
		var passwordInfo = await _playerRepository
			.GetPlayerLoginPasswordInfoAsync(query.PlayerCode)
			.ConfigureAwait(false);

		if (!_passwordHandler.Verify(passwordInfo.Password, passwordInfo.Salt, query.OldPassword))
		{
			throw new InvalidCredentialsException("");
		}

		var (newSalt, newPassword) = _passwordHandler.GenerateCredential(query.NewPassword);

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
