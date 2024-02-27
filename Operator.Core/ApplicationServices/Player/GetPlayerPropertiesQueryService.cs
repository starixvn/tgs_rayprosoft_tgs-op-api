using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class GetPlayerPropertiesQueryService(
	IPlayerServiceClient client)
		: IGetService<GetPlayerPropertiesQuery, IImmutableDictionary<string, string>>
{
	private readonly IPlayerServiceClient _playerServiceClient = client ?? throw new ArgumentNullException(nameof(client));

	public ValueTask<IImmutableDictionary<string, string>> GetAsync(GetPlayerPropertiesQuery query,
		CancellationToken cancellationToken = default)
		=> _playerServiceClient.GetPlayerPropertiesAsync(query.PlayerCode, cancellationToken);
}