using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Core.Infrastructures.Security;
using Tgs.ServiceUtility;
using TGS.Services.Operations.Queries;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

internal class CheckWithdrawPayPasswordCommandService(IPlayerQueries playerQueries, IPasswordHandler passwordHandler) : ICommandService<CheckWithdrawPayPasswordCommand>
{
	private readonly IPasswordHandler _passwordHandler = passwordHandler ?? throw new ArgumentNullException(nameof(passwordHandler));
	private readonly IPlayerQueries _playerQueries = playerQueries ?? throw new ArgumentNullException(nameof(playerQueries));

	public async ValueTask ExecuteAsync(
		CheckWithdrawPayPasswordCommand command, CancellationToken cancellationToken = default)
	{
		var (paySalt, payPassword) = await _playerQueries
			.GetPaySaltAndPasswordByPlayerCodeAsync(command.PlayerCode)
			.ConfigureAwait(false);

		var isVaild = _passwordHandler.Verify(payPassword, paySalt, command.PayPassword);

		command.ReturnSource.SetResult(isVaild);
	}
}