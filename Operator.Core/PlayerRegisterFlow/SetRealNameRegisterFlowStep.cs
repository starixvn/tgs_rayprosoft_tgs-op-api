using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;

namespace TGS.Services.Operator.PlayerRegisterFlow;

internal class SetRealNameRegisterFlowStep : IRegisterFlowStep
{
    private readonly IPlayerServiceClient _playerServiceClient;

    public string Type => "SetRealName";

    public SetRealNameRegisterFlowStep(IPlayerServiceClient playerServiceClient)
    {
        _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
    }

    public async ValueTask<bool> ConfirmCompletedAsync(string? playerCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(playerCode))
            return false;

        var props = await _playerServiceClient
            .GetPlayerPropertiesAsync(playerCode, cancellationToken)
            .ConfigureAwait(false);

        return props is not null
            && props.TryGetValue(nameof(PlayerPropertyName.RealName), out var realName)
            && !string.IsNullOrEmpty(realName);
    }

    public async ValueTask<RegisterFlowProcessResult> ProcessInputAsync(
        Guid brandId,
        JsonDocument input,
        PlayerLocationInfo locationInfo,
        CancellationToken cancellationToken = default)
    {
        var request = input.Deserialize<RealNameData>()!;

        if (string.IsNullOrWhiteSpace(locationInfo.PlayerCode))
            return new RegisterFlowProcessResult(false, null, null);

        await _playerServiceClient.SetPlayerPropertiesAsync(
            locationInfo.PlayerCode,
            new Dictionary<PlayerPropertyName, string?>
            {
                [PlayerPropertyName.RealName] = request.RealName
            }.ToImmutableDictionary(),
            cancellationToken).ConfigureAwait(false);

        return new RegisterFlowProcessResult(
            await ConfirmCompletedAsync(
                locationInfo.PlayerCode,
                cancellationToken).ConfigureAwait(false),
            null,
            null);
    }

    internal record RealNameData
    {
        [JsonPropertyName("realname")]
        public string RealName { get; init; } = default!;
    }
}
