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

public class CommitInitPlayerPhoneCommandService(
	IValidateCodeOperator validateCodeOperator,
	IRedisCacheManager cacheManager,
	IPlayerServiceClient playerServiceClient) : ICommandService<CommitInitPlayerPhoneCommand>
{
	private readonly IRedisCacheManager _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
	private readonly IPlayerServiceClient _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
	private readonly IValidateCodeOperator _validateCodeOperator =
			validateCodeOperator ?? throw new ArgumentNullException(nameof(validateCodeOperator));

	public async ValueTask ExecuteAsync(CommitInitPlayerPhoneCommand command,
		CancellationToken cancellationToken = default)
	{
		var result = await _validateCodeOperator
			.GetValidateCodeDataAsync<PhoneValidateData>(command.ValidateCode, command.SecurityKey)
			.ConfigureAwait(false) 
			?? throw new ValidateCodeNotVerifyException();

		var playerCode = result.PlayerCode;

		var findPlayer = await _playerServiceClient.GetPlayerInfoByPhoneAsync(
			command.BrandId,
			result.AreaCode,
			result.Phone,
			cancellationToken).ConfigureAwait(false);

		var isBrandHasSamePhone = findPlayer is not null && findPlayer.PlayerCode != playerCode;

		if (isBrandHasSamePhone)
			throw new BrandPhoneDuplicateException();

		await _playerServiceClient.SetPlayerPropertiesAsync(
			result.PlayerCode,
			new Dictionary<PlayerPropertyName, string?>
			{
				[PlayerPropertyName.AreaCode] = result.AreaCode,
				[PlayerPropertyName.Phone] = result.Phone,
				[PlayerPropertyName.PhoneAuthentication] = true.ToString()
			}.ToImmutableDictionary(),
			null,
			cancellationToken)
			.ConfigureAwait(false);

		await _cacheManager
			.ClearCacheAsync(RedisCacheType.Player, playerCode)
			.ConfigureAwait(false);
	}
}
