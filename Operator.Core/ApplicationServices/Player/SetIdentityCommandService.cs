using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using TGS.Services.Operations;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class SetIdentityCommandService(IPlayerOperations playerOperations) : ICommandService<SetIdentityCommand>
{
	private readonly IPlayerOperations _playerOperations = playerOperations ?? throw new ArgumentNullException(nameof(playerOperations));

	public async ValueTask ExecuteAsync(SetIdentityCommand command, CancellationToken cancellationToken = default)
	{
		var updatedOn = DateTimeOffset.UtcNow;

		await _playerOperations.SetIdentityAsync(
			command.PlayerCode,
			command.RealName,
			command.Identity,
			false,
			updatedOn)
			.ConfigureAwait(false);
	}
}