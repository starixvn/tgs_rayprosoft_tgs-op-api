using System;
using System.Threading.Tasks;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Core.Models.Services.Caches;
using TGS.Services.Operations.Models.Services;

namespace TGS.Services.Operations;

public interface IPlayerOperations
{
    Task ChangePlayerPasswordAsync(ChangePlayerPasswordData data);

    Task<string> CreatePlayerAsync(CreatePlayerData data);

    Task<Guid> GetBrandIdByPlayerCodeAsync(string playerCode);

    Task<CachedPlayer> GetCachedPlayerAsync(string playerCode);

    Task<Guid> GetDefaultWalletIdByPlayerCodeAsync(string playerCode);

    Task<decimal> GetPlayerBalanceAsync(Guid defaultWalletId, bool dirtyRead);

    Task<Player> GetPlayerByCodeAsync(string playerCode);

    Task<string> GetPlayerCodeByUsernameAsync(Guid brandId, string username);

    Task<string> GetUsernameByPlayerCodeAsync(string playerCode);

    Task SetEmailAsync(string playerCode, string email, DateTimeOffset updatedOn);

    Task SetIdentityAsync(
        string playerCode,
        string realName,
        string identity,
        bool isAuthenticate,
        DateTimeOffset updatedOn);

    Task SetPayPasswordAsync(string playerCode, byte[] newPassword, byte[] newSalt, DateTimeOffset updatedOn);

    Task SetQqAsync(string playerCode, string qqId, DateTimeOffset updatedOn);

    Task SetWeChatAsync(string playerCode, string weChatId, DateTimeOffset updatedOn);
}
