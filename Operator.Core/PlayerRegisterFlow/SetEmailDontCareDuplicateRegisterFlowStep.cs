using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using TGS.Core.Infrastructures.Redis;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator.PlayerRegisterFlow;

internal class SetEmailDontCareDuplicateRegisterFlowStep(
	IPlayerServiceClient playerServiceClient,
	IRedisCacheManager cacheManager) : IRegisterFlowStep
{
	private readonly IRedisCacheManager _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
	private readonly IPlayerServiceClient _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));

	public string Type => "SetEmailDontCareDuplicate";

	public async ValueTask<bool> ConfirmCompletedAsync(string? playerCode, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(playerCode))
			return false;

		var player = await _playerServiceClient
			.GetPlayerInfoAsync(playerCode, cancellationToken)
			.ConfigureAwait(false);

		return player is not null && player.EmailAuthentication;
	}

	public async ValueTask<RegisterFlowProcessResult> ProcessInputAsync(
		Guid brandId,
		JsonDocument input,
		PlayerLocationInfo locationInfo,
		CancellationToken cancellationToken = default)
	{
		var request = input.Deserialize<EmailData>()!;

		if (string.IsNullOrWhiteSpace(locationInfo.PlayerCode))
			return new RegisterFlowProcessResult(false, null, null);

		await _playerServiceClient.SetPlayerPropertiesAsync(
			locationInfo.PlayerCode,
			new Dictionary<PlayerPropertyName, string?>
			{
				[PlayerPropertyName.Email] = request.Email
			}.ToImmutableDictionary(),
			null,
			cancellationToken).ConfigureAwait(false);

		await _cacheManager
			.ClearCacheAsync(RedisCacheType.Player, locationInfo.PlayerCode)
			.ConfigureAwait(false);

		var player = await _playerServiceClient
			.GetPlayerInfoAsync(locationInfo.PlayerCode!, cancellationToken)
			.ConfigureAwait(false);

		return new RegisterFlowProcessResult(
			player is not null && player.EmailAuthentication,
			null,
			null);
	}

	internal record EmailData
	{
		[JsonPropertyName("email")]
		public string Email { get; init; } = default!;
	}
}