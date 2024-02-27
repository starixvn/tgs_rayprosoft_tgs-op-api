using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using TGS.Core.Infrastructures.Redis;
using TGS.Core.Models.Enums;
using TGS.Services.Operator.ValidateCode;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class CommitSetPlayerPhoneCommandService(
	IValidateCodeOperator validateCodeOperator,
	IPlayerRepository playerRepository,
	IRedisCacheManager cacheManager,
	IGetService<CheckBrandPhoneCanUseQuery, bool> checkService) : ICommandService<CommitSetPlayerPhoneCommand>
{
	private readonly IRedisCacheManager _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
	private readonly IPlayerRepository _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
	private readonly IValidateCodeOperator _validateCodeOperator = validateCodeOperator ?? throw new ArgumentNullException(nameof(validateCodeOperator));

	public async ValueTask ExecuteAsync(CommitSetPlayerPhoneCommand command,
		CancellationToken cancellationToken = default)
	{
		var result = await _validateCodeOperator
			.GetValidateCodeDataAsync<PhoneValidateData>(command.ValidateCode, command.SecurityKey)
			.ConfigureAwait(false)
			?? throw new ValidateCodeNotVerifyException();

		var checkQuery = new CheckBrandPhoneCanUseQuery(
			command.BrandId,
			command.PlayerCode,
			result.AreaCode,
			result.Phone);

		var canSetPhone = await checkService
			.GetAsync(checkQuery, cancellationToken)
			.ConfigureAwait(false);

		if (!canSetPhone)
			throw new BrandPhoneDuplicateException();

		await _playerRepository.SetPlayerPhoneAsync(
			command.PlayerCode,
			result.AreaCode,
			result.Phone).ConfigureAwait(false);
		await _cacheManager
			.ClearCacheAsync(RedisCacheType.Player, command.PlayerCode)
			.ConfigureAwait(false);
	}
}