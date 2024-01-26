using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using static TGS.Services.Operator.PlayerRegisterFlow.SetPhoneRegisterFlowStep;

namespace TGS.Services.Operator.PlayerRegisterFlow;

internal class SetPhoneDontCareDuplicateRegisterFlowStep(IPlayerServiceClient playerServiceClient) : IRegisterFlowStep
{
	private readonly IPlayerServiceClient _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
	private bool _compatible;

	public string Type => "SetPhoneDontCareDuplicate";

	public async ValueTask<bool> ConfirmCompletedAsync(string? playerCode, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(playerCode))
			return false;

		var player = await _playerServiceClient
			.GetPlayerInfoAsync(playerCode, cancellationToken)
			.ConfigureAwait(false);

		return player is not null && player.PhoneAuthentication;
	}

	public async ValueTask<RegisterFlowProcessResult> ProcessInputAsync(
		Guid brandId,
		JsonDocument input,
		PlayerLocationInfo locationInfo,
		CancellationToken cancellationToken = default)
	{
		var request = DeserializeInputData(input);

		if (string.IsNullOrWhiteSpace(locationInfo.PlayerCode))
			return new RegisterFlowProcessResult(false, null, null);

		await _playerServiceClient.SetPlayerPropertiesAsync(
			locationInfo.PlayerCode,
			new Dictionary<PlayerPropertyName, string?>
			{
				[PlayerPropertyName.AreaCode] = request.AreaCode,
				[PlayerPropertyName.Phone] = request.Phone,
				[PlayerPropertyName.PhoneAuthentication] = true.ToString()
			}.ToImmutableDictionary(),
			null,
			cancellationToken).ConfigureAwait(false);

		var player = await _playerServiceClient
			.GetPlayerInfoAsync(locationInfo.PlayerCode!, cancellationToken)
			.ConfigureAwait(false);

		return new RegisterFlowProcessResult(
			player is not null && player.PhoneAuthentication,
			null,
			null);
	}

	public void SetupStep(JsonElement jsonElement)
	{
		if (jsonElement.TryGetProperty("compatible", out var prop))
			_compatible = prop.GetBoolean();
	}

	private PhoneData DeserializeInputData(JsonDocument input)
	{
		if (_compatible)
		{
			var inputData = input.Deserialize<InitPhoneStepInputData>()!;

			return inputData.Data.Deserialize<PhoneData>()!;
		}
		else
			return input.Deserialize<PhoneData>()!;
	}

	internal record PhoneData
	{
		[JsonPropertyName("areacode")]
		public string AreaCode { get; init; } = default!;

		[JsonPropertyName("phone")]
		public string Phone { get; init; } = default!;
	}
}
