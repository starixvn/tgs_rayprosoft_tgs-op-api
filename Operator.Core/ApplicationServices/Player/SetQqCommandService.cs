using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using TGS.Services.Operations;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class SetQqCommandService(IPlayerOperations playerOperations) : ICommandService<SetQqCommand>
{
	private readonly IPlayerOperations _playerOperations = playerOperations ?? throw new ArgumentNullException(nameof(playerOperations));

	public async ValueTask ExecuteAsync(SetQqCommand command, CancellationToken cancellationToken = default)
	{
		var updatedOn = DateTimeOffset.UtcNow;
		await _playerOperations
			.SetQqAsync(command.PlayerCode, command.QqId, updatedOn)
			.ConfigureAwait(false);
	}
}
