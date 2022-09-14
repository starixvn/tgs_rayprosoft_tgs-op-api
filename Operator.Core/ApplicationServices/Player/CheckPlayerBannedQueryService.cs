using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using TGS.Services.Operations;
using CoreEnums = TGS.Core.Models.Enums;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class CheckPlayerBannedQueryService : IGetService<CheckPlayerBannedQuery, bool>
{
    private readonly IPlayerOperations _playerOperations;

    public CheckPlayerBannedQueryService(IPlayerOperations playerOperations)
    {
        _playerOperations = playerOperations ?? throw new ArgumentNullException(nameof(playerOperations));
    }

    public async ValueTask<bool> GetAsync(CheckPlayerBannedQuery query, CancellationToken cancellationToken = default)
    {
        var playerCode = await _playerOperations.GetPlayerCodeByUsernameAsync(query.BrandId, query.UserName);
        if (string.IsNullOrEmpty(playerCode))
            return false;

        var player = await _playerOperations.GetPlayerByCodeAsync(playerCode);

        return player.Status == CoreEnums.PlayerStatus.InBlacklist;
    }
}
