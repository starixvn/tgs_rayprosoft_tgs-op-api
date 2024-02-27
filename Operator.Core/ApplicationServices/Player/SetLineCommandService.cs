using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

public class SetLineCommandService(IPlayerServiceClient client) : ICommandService<SetLineCommand>
{
	private readonly IPlayerServiceClient _client = client ?? throw new ArgumentNullException(nameof(client));

	public ValueTask ExecuteAsync(SetLineCommand command, CancellationToken cancellationToken = default)
	{
		var properties = new Dictionary<PlayerPropertyName, string?>
		{
			[PlayerPropertyName.Line] = command.LineId
		}.ToImmutableDictionary();

		return _client.SetPlayerPropertiesAsync(
			command.PlayerCode,
			properties,
			null,
			cancellationToken);
	}
}