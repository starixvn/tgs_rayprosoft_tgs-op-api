using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.GameProviders.Pluggable.Client;
using Tgs.ServiceUtility;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Extensions;
using TGS.Services.Operator.GameProviders;

namespace TGS.Services.Operator.ApplicationServices.Game;

internal class GetGameUrlQueryService(
	IGameProvidersOperatorPluggableClient pluggableClient,
	IGameProviderRepository gameProviderRepository) : IGetService<GetGameUrlQuery, Uri?>
{
	private readonly IGameProviderRepository _gameProviderRepository = gameProviderRepository ?? throw new ArgumentNullException(nameof(gameProviderRepository));
	private readonly IGameProvidersOperatorPluggableClient _pluggableClient = pluggableClient ?? throw new ArgumentNullException(nameof(pluggableClient));

	public async ValueTask<Uri?> GetAsync(
		GetGameUrlQuery query,
		CancellationToken cancellationToken = default)
	{
		var playerTokenData = query.PlayerTokenData;

		var game = await _gameProviderRepository
			.QueryAllGamesAsync(query.BrandId)
			.Where(g => g.Id == query.GameId)
			.FirstOrDefaultAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var allowedGamePlatformTypes = playerTokenData.PlatformType.ToGamePlatformTypes();
		if (!allowedGamePlatformTypes.Contains(game!.PlatformType))
		{
			var allowPlatforms = string.Join(",", allowedGamePlatformTypes);
			var msg =
				$"Launch game platform is expected as '{allowPlatforms}' " +
				$"but is {game.PlatformType} with player login as '{playerTokenData.PlatformType}'";

			throw new InvalidOperationException(msg);
		}

		Uri? backUrl = null;

		var extendedData = query.ExtendedData;

		if (extendedData != null &&
			extendedData.RootElement.TryGetProperty("lobbyurl", out var prop))
		{
			var lobbyUrl = prop.GetString();
			if (!string.IsNullOrEmpty(lobbyUrl))
			{
				backUrl = new Uri(lobbyUrl);
			}
		}

		var gameUrl = await _pluggableClient
			.GetGameUrlAsync(
				playerTokenData.PlayerCode,
				playerTokenData.LoginRecordId,
				playerTokenData.PlatformType == PlatformType.Mobile,
				query.GameId,
				query.LanguageCode,
				query.IpAddress,
				backUrl,
				cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return gameUrl;
	}
}
