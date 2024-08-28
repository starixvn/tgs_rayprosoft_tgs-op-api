using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class SetWeChatCommandService(IPlayerServiceClient client) : ICommandService<SetWeChatCommand>
{
	public ValueTask ExecuteAsync(SetWeChatCommand command, CancellationToken cancellationToken = default)
	{
		var properties = new Dictionary<PlayerPropertyName, string?>
		{
			[PlayerPropertyName.Wechat] = command.WeChatId,
		}.ToImmutableDictionary();

		return client.SetPlayerPropertiesAsync(
			command.PlayerCode,
			properties,
			null,
			cancellationToken);
	}
}