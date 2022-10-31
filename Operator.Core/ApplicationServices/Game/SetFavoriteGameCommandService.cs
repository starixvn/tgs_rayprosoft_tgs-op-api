using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using TGS.Services.Operator.GameProviders;

namespace TGS.Services.Operator.ApplicationServices.Game;

internal class SetFavoriteGameCommandService : ICommandService<SetFavoriteGameCommand>
{
	private readonly IGameProviderRepository _gameProviderRepository;

	public SetFavoriteGameCommandService(
		IGameProviderRepository gameProviderRepository)
	{
		_gameProviderRepository = gameProviderRepository ?? throw new ArgumentNullException(nameof(gameProviderRepository));
	}

	public async ValueTask ExecuteAsync(SetFavoriteGameCommand command, CancellationToken cancellationToken = default)
	{
		var gameInfo = await _gameProviderRepository.QueryAllGamesAsync(command.BrandId)
			.Where(g => g.Id == command.GameId)
			.FirstOrDefaultAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		if (gameInfo == null)
			return;

		var favoriteInfo = new FavoriteGameInfo(
			command.PlayerCode,
			gameInfo.Id,
			gameInfo.Type,
			gameInfo.SubGpCode);

		await _gameProviderRepository
			.AddFavoriteGameAsync(command.BrandId, favoriteInfo)
			.ConfigureAwait(false);
	}
}
