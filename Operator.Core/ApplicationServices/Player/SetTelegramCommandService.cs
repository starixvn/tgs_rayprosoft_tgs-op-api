using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class SetTelegramCommandService : ICommandService<SetTelegramCommand>
{
	private readonly IPlayerServiceClient _client;

	public SetTelegramCommandService(IPlayerServiceClient client)
	{
		_client = client ?? throw new ArgumentNullException(nameof(client));
	}

	public ValueTask ExecuteAsync(SetTelegramCommand command, CancellationToken cancellationToken = default)
	{
		var properties = new Dictionary<PlayerPropertyName, string?>
		{
			[PlayerPropertyName.Telegram] = command.TelegramId
		}.ToImmutableDictionary();

		return _client.SetPlayerPropertiesAsync(command.PlayerCode, properties, cancellationToken);
	}
}
