using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class SetQqCommandService(IPlayerServiceClient client) : ICommandService<SetQqCommand>
{
	public ValueTask ExecuteAsync(SetQqCommand command, CancellationToken cancellationToken = default)
	{
		var properties = new Dictionary<PlayerPropertyName, string?>
		{
			[PlayerPropertyName.Qq] = command.QqId
		}.ToImmutableDictionary();

		return client.SetPlayerPropertiesAsync(
			command.PlayerCode,
			properties,
			null,
			cancellationToken);
	}
}