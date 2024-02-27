using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using TGS.Core.Models.Extensions;
using TGS.Services.Operator.GameProviders;

namespace TGS.Services.Operator.ApplicationServices.Game;

internal class GetLobbyGameUrlQueryService(
	IGameProviderRepository gameProviderRepository,
	IGetService<GetGameUrlQuery, Uri?> getGameUrlQueryService) : IGetService<GetLobbyGameUrlQuery, Uri?>
{
	private readonly IGameProviderRepository _gameProviderRepository = gameProviderRepository ?? throw new ArgumentNullException(nameof(gameProviderRepository));
	private readonly IGetService<GetGameUrlQuery, Uri?> _getGameUrlQueryService = getGameUrlQueryService ?? throw new ArgumentNullException(nameof(getGameUrlQueryService));

	public async ValueTask<Uri?> GetAsync(GetLobbyGameUrlQuery query, CancellationToken cancellationToken = default)
	{
		var playerTokenData = query.PlayerTokenData;
		var platformType = playerTokenData.PlatformType;
		var gameType = query.GameType;

		var gamePlatformTypes = platformType.ToGamePlatformTypes();

		var games = await _gameProviderRepository
			.QueryAllGamesAsync(query.BrandId)
			.Where(g => g.GpId == query.GameProviderId
				&& gamePlatformTypes.Contains(g.PlatformType)
				&& g.Type == query.GameType)
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		if (games.Length != 1)
		{
			throw new InvalidOperationException(
			$"Game Provider '{games[0].GpCode}' with game type '{gameType}' and platform '{platformType}' should only have 1 lobby but found {games.Length}.");
		}

		var gameId = games[0].Id;

		var getGameUrlQuery = new GetGameUrlQuery(
			query.BrandId,
			playerTokenData,
			gameId,
			query.LanguageCode,
			query.IpAddress,
			query.Extends);

		return await _getGameUrlQueryService
			.GetAsync(getGameUrlQuery, cancellationToken)
			.ConfigureAwait(false);
	}
}