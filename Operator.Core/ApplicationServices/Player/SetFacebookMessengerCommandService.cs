using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class SetFacebookMessengerCommandService(IPlayerServiceClient client) : ICommandService<SetFacebookMessengerCommand>
{
	private readonly IPlayerServiceClient _client = client ?? throw new ArgumentNullException(nameof(client));

	public ValueTask ExecuteAsync(SetFacebookMessengerCommand command, CancellationToken cancellationToken = default)
	{
		var properties = new Dictionary<PlayerPropertyName, string?>
		{
			[PlayerPropertyName.FacebookMessenger] = command.FacebookMessengerId
		};

		return _client.SetPlayerPropertiesAsync(
			command.PlayerCode,
			properties.ToImmutableDictionary(),
			null,
			cancellationToken);
	}
}