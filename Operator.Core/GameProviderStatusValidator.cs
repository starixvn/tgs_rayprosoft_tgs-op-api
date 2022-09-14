using System;
using System.Linq;
using System.Threading.Tasks;
using TGS.Core.Models;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Exceptions;
using TGS.Core.Models.Tokens;
using TGS.Services.Operations;

namespace TGS.Services.Operator;

internal class GameProviderStatusValidator : IGameProviderStatusValidator
{
    private readonly IBrandOperations _brandOperations;
    private readonly IGameProviderOperations _gameProviderOperations;
    private readonly IPlayerOperations _playerOperations;
    private readonly IValidStatus<Status> _validStatus;

    public GameProviderStatusValidator(
        IGameProviderOperations gameProviderOperations,
        IPlayerOperations playerOperations,
        IValidStatus<Status> validStatus,
        IBrandOperations brandOperations)
    {
        _gameProviderOperations = gameProviderOperations;
        _playerOperations = playerOperations;
        _validStatus = validStatus;
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

        var cachedPlayer = await _playerOperations.GetCachedPlayerAsync(playerTokenData.PlayerCode);
        var playerType = cachedPlayer.Type;
        var statuses = _validStatus.GetStatus(playerType);

        ValidateGameProviderAndGameStatus(statuses, gameProviderStatus.Value);

        var brandGameProviderStatus =
            await _brandOperations.GetBrandGameProviderStatusAsync(brandId, gameProviderId);

        if (brandGameProviderStatus == null)
        {
            throw new InvalidStatusException("Brand doesn't have the configuration of GameProvider");
        }

        ValidateGameProviderAndGameStatus(statuses, brandGameProviderStatus.Value);
    }

    private static void ValidateGameProviderAndGameStatus(Status[] statuses, Status gameProviderStatus)
    {
        if (!statuses.Contains(Status.Maintenance) && gameProviderStatus == Status.Maintenance)
        {
            throw new GameProviderUnderMaintenanceException();
        }

        if (!statuses.Contains(gameProviderStatus))
        {
            throw new InvalidStatusException("GameProvider doesn't exist or is not active");
        }
    }
}
