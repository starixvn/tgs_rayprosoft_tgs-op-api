using System;
using System.Threading.Tasks;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Core.Models.Enums;
using TGS.Services.Operations.Models.Services;

namespace TGS.Services.Operations.Commands;

public interface IPlayerCommands
{
    Task<string> CreatePlayerAsync(CreatePlayerData data);

    Task ChangePlayerPasswordAsync(ChangePlayerPasswordData data);

    Task<PlayerLoginRecord> PlayerLoginAsync(PlayerLoginData data);

    Task DeauthorizePlayerAsync(string playerCode, Guid loginRecordId);

    Task DeauthorizePlayerAsync(string playerCode, PlatformType? platformType);

    Task SetPayPasswordAsync(string playerCode, byte[] newPassword, byte[] newSalt, DateTimeOffset updatedOn);

    Task<int> SetEmailAsync(string playerCode, string email, DateTimeOffset updatedOn);

    Task SetPhoneAsync(string playerCode, string phone, DateTimeOffset updatedOn);

    Task SetIdentityAsync(
        string playerCode,
        string realName,
        string identity,
        bool isAuthenticate,
        DateTimeOffset updatedOn);

    Task SetQqAsync(string playerCode, string qqId, DateTimeOffset updatedOn);

    Task SetWeChatAsync(string playerCode, string weChatId, DateTimeOffset updatedOn);
}