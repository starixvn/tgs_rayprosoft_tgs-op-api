using System;
using System.Linq;
using System.Threading.Tasks;
using TGS.Core.Infrastructures.Extensions;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Exceptions;

namespace TGS.Services.Operations.Validators;

public sealed class StatusValidator : IStatusValidator
{
    private readonly IBrandOperations _brandOperations;
    private readonly IGameProviderOperations _gameProviderOperations;

    public StatusValidator(
        IBrandOperations brandOperations,
        IGameProviderOperations gameProviderOperations)
    {
        _brandOperations = brandOperations;
        _gameProviderOperations = gameProviderOperations;
    }

    public async Task ThrowIfBrandStatusIsNotOneOfAsync(Guid brandId, params Status[] statuses)
    {
        EnsureStatus(statuses);

        var status = await _brandOperations.GetBrandStatusAsync(brandId);

        if (status == null)
        {
            throw new InvalidStatusException("Brand doesn't exist");
        }

        if (!statuses.Contains(Status.Maintenance) && status == Status.Maintenance)
        {
            throw new BrandUnderMaintenanceException("Brand is put into maintenance");
        }

        if (!statuses.Contains(status.Value))
        {
            throw new InvalidStatusException("Brand doesn't exist or is not active");
        }
    }

    public async Task ThrowIfGameOrGameProviderStatusIsNotOneOfAsync(
        Guid brandId,
        Guid gameProviderId,
        int gameId,
        string idToThrow,
        Status[] statuses,
        GameStatus[] gameStatuses)
    {
        EnsureStatus(statuses);
        EnsureStatus(gameStatuses);

        var gameProviderStatus = await _gameProviderOperations.GetGameProviderStatusAsync(gameProviderId);
        var cachedGame = await _gameProviderOperations.GetCachedGameAsync(gameProviderId, gameId);

        if (gameProviderStatus == null || cachedGame == null)
        {
            throw new InvalidStatusException("GameProvider or Game doesn't exist");
        }

        ValidateGameProviderAndGameStatus(gameId, idToThrow, statuses, gameStatuses, gameProviderStatus.Value,
            cachedGame.Status);

        var brandGameProviderStatus =
            await _brandOperations.GetBrandGameProviderStatusAsync(brandId, gameProviderId);

        var brandGameStatus = cachedGame.SubGame && !cachedGame.AvailableToBrand
            ? cachedGame.Status
            : await _brandOperations.GetBrandGameStatusAsync(brandId, gameProviderId, gameId);

        if (brandGameProviderStatus == null || brandGameStatus == null)
        {
            throw new InvalidStatusException("Brand doesn't have the configuration of GameProvider or Game");
        }

        ValidateGameProviderAndGameStatus(gameId, idToThrow, statuses, gameStatuses, brandGameProviderStatus.Value,
            brandGameStatus.Value);
    }

    public async Task ThrowIfGameProviderOrConfigurationStatusIsNotOneOfAsync(
        Guid gameProviderId,
        Guid gameProviderConfigurationId,
        params Status[] statuses)
    {
        EnsureStatus(statuses);

        var gameProviderStatus = await _gameProviderOperations.GetGameProviderStatusAsync(gameProviderId);
        var configurationStatus =
            await _gameProviderOperations.GetGameProviderConfigurationStatusAsync(gameProviderId,
                gameProviderConfigurationId);

        if (gameProviderStatus == null || configurationStatus == null)
        {
            throw new InvalidStatusException("GameProvider or GameProviderConfiguration doesn't exist");
        }

        if (!statuses.Contains(Status.Maintenance)
            && (gameProviderStatus == Status.Maintenance
                || configurationStatus == Status.Maintenance))
        {
            throw new GameProviderUnderMaintenanceException();
        }

        if (!statuses.Contains(gameProviderStatus.Value) || !statuses.Contains(configurationStatus.Value))
        {
            throw new InvalidStatusException(
                "GameProvider or GameProviderConfiguration doesn't exist or is not active");
        }
    }

    public void ThrowIfPlayerDisabled(PlayerStatus status) =>
        EnsurePlayerStatus(status, PlayerStatus.Disabled);

    public void ThrowIfPlayerDisabledToPlaceBet(PlayerStatus status) =>
        EnsurePlayerStatus(status, PlayerStatus.InBlacklist);

    #region Private Methods

    private static void EnsurePlayerStatus(PlayerStatus playerStatus, PlayerStatus statusToThrowException)
    {
        if (playerStatus == PlayerStatus.Enabled)
        {
            return;
        }

        if (playerStatus == statusToThrowException)
        {
            throw new PlayerDisabledException(playerStatus);
        }
    }

    private static void EnsureStatus<T>(T[] status) where T : struct
    {
        if (status == null || status.Length == 0)
        {
            throw new ArgumentException("No status provided");
        }
    }

    private static void ValidateGameProviderAndGameStatus(int gameId, string idToThrow, Status[] statuses,
        GameStatus[] gameStatuses, Status gameProviderStatus, GameStatus gameStatus)
    {
        if (!statuses.Contains(Status.Maintenance) && gameProviderStatus == Status.Maintenance)
        {
            throw new GameProviderUnderMaintenanceException();
        }

        if (!statuses.Contains(gameProviderStatus))
        {
            throw new InvalidStatusException("GameProvider doesn't exist or is not active");
        }

        if (!gameStatuses.Contains(GameStatus.Maintenance) && gameStatus == GameStatus.Maintenance)
        {
            throw new GameUnderMaintenanceException();
        }

        if (!gameStatuses.Contains(gameStatus))
        {
            var id = idToThrow.IsNullOrEmpty() ? gameId.ToString() : idToThrow;
            throw new InvalidStatusException($"Game '{id}' doesn't exist or is not active");
        }
    }

    #endregion Private Methods
}
