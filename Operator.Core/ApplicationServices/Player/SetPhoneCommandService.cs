using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Services.Operations;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class SetPhoneCommandService(IPlayerOperations playerOperations) : ICommandService<SetPhoneCommand>
{
	private readonly IPlayerOperations _playerOperations = playerOperations ?? throw new ArgumentNullException(nameof(playerOperations));

	public async ValueTask ExecuteAsync(SetPhoneCommand command, CancellationToken cancellationToken = default)
	{
		var updatedOn = DateTimeOffset.UtcNow;
		await _playerOperations
			.SetPhoneAsync(command.PlayerCode, command.AreaCode, command.Phone, updatedOn)
			.ConfigureAwait(false);
	}
}