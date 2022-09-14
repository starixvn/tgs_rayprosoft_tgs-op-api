using System;
using System.Threading.Tasks;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operations.Validators;

public interface IStatusValidator
{
    Task ThrowIfBrandStatusIsNotOneOfAsync(Guid brandId, params Status[] statuses);

    Task ThrowIfGameOrGameProviderStatusIsNotOneOfAsync(
        Guid brandId,
        Guid gameProviderId,
        int gameId,
        string idToThrow,
        Status[] statuses,
        GameStatus[] gameStatuses);

    Task ThrowIfGameProviderOrConfigurationStatusIsNotOneOfAsync(
        Guid gameProviderId,
        Guid gameProviderConfigurationId,
        params Status[] statuses);

    void ThrowIfPlayerDisabled(PlayerStatus playerStatus);

    void ThrowIfPlayerDisabledToPlaceBet(PlayerStatus status);
}
