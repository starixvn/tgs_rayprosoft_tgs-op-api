using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class SetBirthdateCommandService : ICommandService<SetBirthdateCommand>
{
	private readonly IPlayerServiceClient _playerServiceClient;

	public SetBirthdateCommandService(IPlayerServiceClient playerServiceClient)
	{
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
	}

	public ValueTask ExecuteAsync(SetBirthdateCommand command, CancellationToken cancellationToken = default)
	{
		var properties = new Dictionary<PlayerPropertyName, string?>
		{
			[PlayerPropertyName.Birthdate] = command.Birthdate?.ToString("yyyy/MM/dd")
		}.ToImmutableDictionary();

		return _playerServiceClient.SetPlayerPropertiesAsync(
			command.PlayerCode,
			properties,
			null,
			cancellationToken);
	}
}
