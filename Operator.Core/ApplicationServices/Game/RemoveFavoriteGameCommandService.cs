using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using TGS.Services.Operator.GameProviders;

namespace TGS.Services.Operator.ApplicationServices.Game;

internal class RemoveFavoriteGameCommandService : ICommandService<RemoveFavoriteGameCommand>
{
	private readonly IGameProviderRepository _gameProviderRepository;

	public RemoveFavoriteGameCommandService(IGameProviderRepository gameProviderRepository)
	{
		_gameProviderRepository = gameProviderRepository ?? throw new ArgumentNullException(nameof(gameProviderRepository));
	}

	public ValueTask ExecuteAsync(RemoveFavoriteGameCommand command, CancellationToken cancellationToken = default)
		=> _gameProviderRepository.RemoveFavoriteGameAsync(command.PlayerCode, command.GameId);
}
