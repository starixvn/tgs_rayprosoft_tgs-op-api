using System;
using System.Threading.Tasks;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator;

public interface IPlayerRepository
{
    ValueTask<Guid> CreatePlayerAuthenticationFormAsync(Guid brandId, string playerCode, string data);

    ValueTask<bool> GetPlayerAutoTransferFlagAsync(string playerCode);

    ValueTask<PlayerIdentityFormInfo> GetPlayerIdentityFormQueryAsync(string playerCode);

    ValueTask<PasswordInfo> GetPlayerLoginPasswordInfoAsync(string playerCode);

    ValueTask PlayerLoginIpAsync(string playerCode, string ip, string host);

    ValueTask PlayerRegisterIpAsync(string playerCode, string ip, string? host, DateTimeOffset registeredOn);

    ValueTask SetPlayerEmailAsync(string playerCode, string email);

    ValueTask SetPlayerPhoneAsync(string playerCode, string areaCode, string phone);

    ValueTask TogglePlayerAutoTransferFlagAsync(string playerCode);

    ValueTask UpdatePlayerIdentityAsync(string playerCode, Guid identityAuthenticationFormId);

    ValueTask UpdatePlayerLoginPasswordAsync(string playerCode, byte[] salt, byte[] password);

    ValueTask UpdatePlayerStatusAsync(string playerCode, PlayerStatus playerStatus);
}
