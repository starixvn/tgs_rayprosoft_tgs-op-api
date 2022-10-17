using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;

namespace TGS.Services.Operator.PlayerRegisterFlow;

internal class FinalRegisterFlowStep : IRegisterFlowStep
{
	private readonly IPlayerServiceClient _playerServiceClient;

	public bool IsAuto => true;

	public string Type => "Final";

	public FinalRegisterFlowStep(IPlayerServiceClient playerServiceClient)
	{
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
	}

	public async ValueTask<bool> ConfirmCompletedAsync(string? playerCode, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(playerCode))
			return false;

		var playerStatus = await _playerServiceClient
			.GetPlayerStatusAsync(playerCode, cancellationToken)
			.ConfigureAwait(false);

		return playerStatus is not PlayerStatus.RegisterUndone;
	}

	public async ValueTask<RegisterFlowProcessResult> ProcessInputAsync(
		Guid brandId,
		JsonDocument input,
		PlayerLocationInfo locationInfo,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(locationInfo.PlayerCode))
			return new RegisterFlowProcessResult(true, null, null);

		var player = await _playerServiceClient
			.GetPlayerInfoAsync(locationInfo.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		if (player is null)
			return new RegisterFlowProcessResult(true, null, null);

		await _playerServiceClient.SetPlayerStatusAsync(
			locationInfo.PlayerCode,
			PlayerStatus.Enabled,
			cancellationToken).ConfigureAwait(false);

		await _playerServiceClient.RecordRegisterIpAsync(
			locationInfo.PlayerCode,
			locationInfo.Ip,
			locationInfo.HostName,
			cancellationToken).ConfigureAwait(false);

		return new RegisterFlowProcessResult(true, null, null);
	}
}
