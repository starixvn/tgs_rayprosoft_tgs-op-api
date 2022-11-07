using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using TGS.Services.Operations;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class SetWeChatCommandService : ICommandService<SetWeChatCommand>
{
	private readonly IPlayerOperations _playerOperations;

	public SetWeChatCommandService(IPlayerOperations playerOperations)
	{
		_playerOperations = playerOperations ?? throw new ArgumentNullException(nameof(playerOperations));
	}

	public async ValueTask ExecuteAsync(SetWeChatCommand command, CancellationToken cancellationToken = default)
	{
		var updatedOn = DateTimeOffset.UtcNow;
		await _playerOperations
			.SetWeChatAsync(command.PlayerCode, command.WeChatId, updatedOn)
			.ConfigureAwait(false);
	}
}
