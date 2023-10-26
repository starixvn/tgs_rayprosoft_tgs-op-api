using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class SetWhatsAppCommandService : ICommandService<SetWhatsAppCommand>
{
	private readonly IPlayerServiceClient _client;

	public SetWhatsAppCommandService(IPlayerServiceClient client)
	{
		_client = client ?? throw new ArgumentNullException(nameof(client));
	}

	public ValueTask ExecuteAsync(SetWhatsAppCommand command, CancellationToken cancellationToken = default)
	{
		var properties = new Dictionary<PlayerPropertyName, string?>
		{
			[PlayerPropertyName.WhatsApp] = command.WhatsAppId
		}.ToImmutableDictionary();

		return _client.SetPlayerPropertiesAsync(
			command.PlayerCode,
			properties,
			null,
			cancellationToken);
	}
}
