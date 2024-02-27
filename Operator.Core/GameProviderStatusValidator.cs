using System;
using System.Threading.Tasks;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Exceptions;
using TGS.Core.Models.Tokens;
using TGS.Services.Operations;

namespace TGS.Services.Operator;

internal class GameProviderStatusValidator(
	IGameProviderOperations gameProviderOperations,
	IBrandOperations brandOperations) : IGameProviderStatusValidator
{
	public async ValueTask CheckGameProviderStatusAsync(PlayerTokenData playerTokenData, string gameProviderCode, Guid brandId)
	{
		var gameProviderId = await gameProviderOperations.GetGameProviderIdByCodeAsync(gameProviderCode);
		var gameProviderStatus = await gameProviderOperations.GetGameProviderStatusAsync(gameProviderId)
			?? throw new InvalidStatusException("GameProvider doesn't exist");

		ValidateGameProviderStatus(gameProviderStatus);

		var brandGameProviderStatus =
			await brandOperations.GetBrandGameProviderStatusAsync(brandId, gameProviderId)
			?? throw new InvalidStatusException("Brand doesn't have the configuration of GameProvider");

		ValidateGameProviderStatus(brandGameProviderStatus);
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