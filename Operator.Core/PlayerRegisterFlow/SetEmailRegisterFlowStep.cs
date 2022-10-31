using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Tgs.BrandCertificate.Clients;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Services.Operator.ApplicationServices.Player;

namespace TGS.Services.Operator.PlayerRegisterFlow;

internal class SetEmailRegisterFlowStep : IRegisterFlowStep
{
	private readonly IBrandServiceClient _brandServiceClient;
	private readonly ICommandService<CommitInitPlayerEmailCommand> _commitPlayerEmailService;
	private readonly IPlayerServiceClient _playerServiceClient;
	private readonly IGetService<StartSetPlayerEmailCommand, string> _setPlayerEmailService;
	public string Type => "InitMail";

	public SetEmailRegisterFlowStep(
		IBrandServiceClient brandServiceClient,
		IGetService<StartSetPlayerEmailCommand, string> setPlayerEmailService,
		ICommandService<CommitInitPlayerEmailCommand> commitPlayerEmailService,
		IPlayerServiceClient playerServiceClient)
	{
		_brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
		_setPlayerEmailService = setPlayerEmailService ?? throw new ArgumentNullException(nameof(setPlayerEmailService));
		_commitPlayerEmailService = commitPlayerEmailService ?? throw new ArgumentNullException(nameof(commitPlayerEmailService));
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
	}

	public async ValueTask<bool> ConfirmCompletedAsync(string? playerCode, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(playerCode))
			return false;

		var player = await _playerServiceClient
			.GetPlayerInfoAsync(playerCode, cancellationToken)
			.ConfigureAwait(false);

		return player is not null && player.EmailAuthentication;
	}

	public ValueTask<RegisterFlowProcessResult> ProcessInputAsync(Guid brandId, JsonDocument input, PlayerLocationInfo locationInfo, CancellationToken cancellationToken = default)
	{
		var request = input.Deserialize<InitEmailStepInputData>()!;

		return request.InputType == "Valid"
			? ProcessValidEmailAsync(
				brandId,
				request.Data.Deserialize<ConfirmEmailData>()!,
				locationInfo,
				cancellationToken)
			: ProcessSetEmailAsync(
				brandId,
				request.Data.Deserialize<EmailData>()!,
				locationInfo,
				cancellationToken);
	}

	private async ValueTask<RegisterFlowProcessResult> ProcessSetEmailAsync(
		Guid brandId,
		EmailData data,
		PlayerLocationInfo locationInfo,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(data.Email))
			return new RegisterFlowProcessResult(false, null, null);

		var brand = await _brandServiceClient
			.GetBrandInfoAsync(brandId, cancellationToken)
			.ConfigureAwait(false);

		if (brand is null)
			return new RegisterFlowProcessResult(false, null, null);

		if (string.IsNullOrWhiteSpace(locationInfo.PlayerCode))
			return new RegisterFlowProcessResult(false, null, null);

		var playerInfo = await _playerServiceClient
			.GetPlayerInfoAsync(locationInfo.PlayerCode, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		if (playerInfo is null)
			return new RegisterFlowProcessResult(false, null, null);

		var findPlayer = await _playerServiceClient
			.GetPlayerInfoByEmailAsync(brandId, data.Email, cancellationToken)
			.ConfigureAwait(false);

		var isBrandHasSameEmail = findPlayer is not null
			&& findPlayer.PlayerCode != locationInfo.PlayerCode;

		if (isBrandHasSameEmail)
			throw new BrandEmailDuplicateException();

		var command = new StartSetPlayerEmailCommand(
			locationInfo.PlayerCode,
			data.Email,
			CultureInfo.CurrentUICulture,
			true);

		var result = await _setPlayerEmailService
			.GetAsync(command, cancellationToken)
			.ConfigureAwait(false);

		return new RegisterFlowProcessResult(
			false,
			null,
			JsonSerializer.SerializeToDocument(result));
	}

	private async ValueTask<RegisterFlowProcessResult> ProcessValidEmailAsync(
		Guid brandId,
		ConfirmEmailData data,
		PlayerLocationInfo locationInfo,
		CancellationToken cancellationToken = default)
	{
		var command = new CommitInitPlayerEmailCommand(
			data.SecurityKey,
			data.ValidateCode,
			brandId);

		await _commitPlayerEmailService
			.ExecuteAsync(command, cancellationToken)
			.ConfigureAwait(false);

		var player = await _playerServiceClient
			.GetPlayerInfoAsync(locationInfo.PlayerCode!, cancellationToken)
			.ConfigureAwait(false);

		return new RegisterFlowProcessResult(
			player is not null && player.EmailAuthentication,
			null,
			null);
	}

	internal record InitEmailStepInputData
	{
		[JsonPropertyName("input")]
		public string InputType { get; init; } = "Init";

		[JsonPropertyName("data")]
		public JsonDocument Data { get; init; } = default!;
	};

	internal record EmailData
	{
		[JsonPropertyName("email")]
		public string Email { get; init; } = default!;
	}

	internal record ConfirmEmailData
	{
		[JsonPropertyName("securitykey")]
		public string SecurityKey { get; init; } = default!;

		[JsonPropertyName("validatecode")]
		public string ValidateCode { get; init; } = default!;
	}
}
