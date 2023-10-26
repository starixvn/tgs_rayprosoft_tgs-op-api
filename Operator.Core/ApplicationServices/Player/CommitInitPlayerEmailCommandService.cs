using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Core.Infrastructures.Redis;
using TGS.Core.Models.Enums;
using TGS.Services.Operator.ValidateCode;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class CommitInitPlayerEmailCommandService : ICommandService<CommitInitPlayerEmailCommand>
{
	private readonly IRedisCacheManager _cacheManager;
	private readonly IPlayerServiceClient _playerServiceClient;
	private readonly IValidateCodeOperator _validateCodeOperator;

	public CommitInitPlayerEmailCommandService(
	   IPlayerServiceClient playerServiceClient,
	   IValidateCodeOperator validateCodeOperator,
	   IRedisCacheManager cacheManager)
	{
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_validateCodeOperator = validateCodeOperator ?? throw new ArgumentNullException(nameof(validateCodeOperator));
		_cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
	}

	public async ValueTask ExecuteAsync(
		CommitInitPlayerEmailCommand command,
		CancellationToken cancellationToken = default)
	{
		var result = await _validateCodeOperator
			.GetValidateCodeDataAsync<EmailValidateData>(
				command.ValidateCode,
				command.SecurityKey)
			.ConfigureAwait(false)
			?? throw new ValidateCodeNotVerifyException();

		var playerCode = result.PlayerCode;

		var findPlayer = await _playerServiceClient
			.GetPlayerInfoByEmailAsync(command.BrandId, result.Email, cancellationToken)
			.ConfigureAwait(false);

		var isBrandHasSameEmail = findPlayer is not null && findPlayer.PlayerCode != playerCode;

		if (isBrandHasSameEmail)
			throw new BrandEmailDuplicateException();

		await _playerServiceClient
			.SetPlayerPropertiesAsync(
				playerCode,
				new Dictionary<PlayerPropertyName, string?>
				{
					[PlayerPropertyName.Email] = result.Email
				}.ToImmutableDictionary(),
				null,
				cancellationToken)
			.ConfigureAwait(false);

		await _cacheManager
			.ClearCacheAsync(RedisCacheType.Player, playerCode)
			.ConfigureAwait(false);
	}
}
