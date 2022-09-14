using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using TGS.Core.Infrastructures.Redis;
using TGS.Core.Models.Enums;
using TGS.Services.Operator.ValidateCode;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class CommitSetPlayerPhoneCommandService : ICommandService<CommitSetPlayerPhoneCommand>
{
    private readonly IRedisCacheManager _cacheManager;
    private readonly IGetService<CheckBrandPhoneCanUseQuery, bool> _checkService;
    private readonly IPlayerRepository _playerRepository;
    private readonly IValidateCodeOperator _validateCodeOperator;

    public CommitSetPlayerPhoneCommandService(
        IValidateCodeOperator validateCodeOperator,
        IPlayerRepository playerRepository,
        IRedisCacheManager cacheManager,
        IGetService<CheckBrandPhoneCanUseQuery, bool> checkService)
    {
        _validateCodeOperator = validateCodeOperator ?? throw new ArgumentNullException(nameof(validateCodeOperator));
        _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        _checkService = checkService;
    }

    public async ValueTask ExecuteAsync(CommitSetPlayerPhoneCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _validateCodeOperator
            .GetValidateCodeDataAsync<PhoneValidateData>(command.ValidateCode, command.SecurityKey)
            .ConfigureAwait(false);

        if (result == null)
            throw new ValidateCodeNotVerifyException();

        var checkQuery = new CheckBrandPhoneCanUseQuery(
            command.BrandId,
            command.PlayerCode,
            result.AreaCode,
            result.Phone);

        var canSetPhone = await _checkService
            .GetAsync(checkQuery)
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
