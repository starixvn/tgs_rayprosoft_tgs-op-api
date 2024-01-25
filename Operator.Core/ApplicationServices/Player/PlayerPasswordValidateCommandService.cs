using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Core.Infrastructures.Security;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

public class PlayerPasswordValidateCommandService(
	IPlayerRepository playerRepository,
	IPasswordHandler passwordHandler) : IGetService<PlayerPasswordValidateCommand, bool>
{
	private readonly IPasswordHandler _passwordHandler = passwordHandler ?? throw new ArgumentNullException(nameof(passwordHandler));
	private readonly IPlayerRepository _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));

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
