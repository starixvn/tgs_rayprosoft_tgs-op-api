using System;
using System.Threading.Tasks;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Exceptions;
using TGS.Core.Models.Tokens;
using TGS.Services.Operations;

namespace TGS.Services.Operator;

internal class GameProviderStatusValidator : IGameProviderStatusValidator
{
	private readonly IBrandOperations _brandOperations;
	private readonly IGameProviderOperations _gameProviderOperations;

	public GameProviderStatusValidator(
		IGameProviderOperations gameProviderOperations,
		IBrandOperations brandOperations)
	{
		_gameProviderOperations = gameProviderOperations;
		_brandOperations = brandOperations;
	}

	public async ValueTask CheckGameProviderStatusAsync(PlayerTokenData playerTokenData, string gameProviderCode, Guid brandId)
	{
		var gameProviderId = await _gameProviderOperations.GetGameProviderIdByCodeAsync(gameProviderCode);
		var gameProviderStatus = await _gameProviderOperations.GetGameProviderStatusAsync(gameProviderId);

		if (gameProviderStatus == null)
		{
			throw new InvalidStatusException("GameProvider doesn't exist");
		}

		ValidateGameProviderStatus(gameProviderStatus.Value);

		var brandGameProviderStatus =
			await _brandOperations.GetBrandGameProviderStatusAsync(brandId, gameProviderId);

		if (brandGameProviderStatus == null)
		{
			throw new InvalidStatusException("Brand doesn't have the configuration of GameProvider");
		}

		ValidateGameProviderStatus(brandGameProviderStatus.Value);
	}

	private static void ValidateGameProviderStatus(Status gameProviderStatus)
	{
		if (gameProviderStatus == Status.Maintenance)
		{
			throw new GameProviderUnderMaintenanceException();
		}

		if (gameProviderStatus != Status.Active)
		{
			throw new InvalidStatusException("GameProvider doesn't exist or is not active");
		}
	}
}
