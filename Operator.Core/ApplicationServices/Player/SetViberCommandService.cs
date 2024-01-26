using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class SetViberCommandService(IPlayerServiceClient client) : ICommandService<SetViberCommand>
{
	private readonly IPlayerServiceClient _client = client ?? throw new ArgumentNullException(nameof(client));

	public ValueTask ExecuteAsync(SetViberCommand command, CancellationToken cancellationToken = default)
	{
		var properties = new Dictionary<PlayerPropertyName, string?>
		{
			[PlayerPropertyName.Viber] = command.ViberId
		}.ToImmutableDictionary();

		return _client.SetPlayerPropertiesAsync(
			command.PlayerCode,
			properties,
			null,
			cancellationToken);
	}
}
