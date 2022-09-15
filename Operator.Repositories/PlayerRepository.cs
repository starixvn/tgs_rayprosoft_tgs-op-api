using System;
using System.Linq;
using System.Threading.Tasks;
using TGS.Core.DataServices.Tgs;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Core.Models.Enums;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Player;

namespace TGS.Operator.Repositories;

internal class PlayerRepository : IPlayerRepository
{
    private readonly ITgsRepository _tgsRepository;

    public PlayerRepository(ITgsRepository tgsRepository)
    {
        _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
    }

    public async ValueTask<Guid> CreatePlayerAuthenticationFormAsync(Guid brandId, string playerCode, string data)
    {
        var createFormId = Guid.NewGuid();
        var currentDate = DateTimeOffset.UtcNow;

        await _tgsRepository.PlayerAuthenticationForms
            .AddAsync(new PlayerAuthenticationForm
            {
                Id = createFormId,
                CreatedOn = currentDate,
                BrandId = brandId,
                PlayerCode = playerCode,
                Data = data
            })
            .ConfigureAwait(false);

        _ = await _tgsRepository
            .SaveChangesAsync()
            .ConfigureAwait(false);

        return createFormId;
    }

    public async ValueTask<bool> GetPlayerAutoTransferFlagAsync(string playerCode)
    {
        var settingData = await _tgsRepository.PlayerGameSettings
            .FindAsync(playerCode)
            .ConfigureAwait(false);

        return settingData?.IsAutoTransferToGame ?? true;
    }

    public ValueTask<PlayerIdentityFormInfo> GetPlayerIdentityFormQueryAsync(string playerCode)
    {
        var query = from playerIdentity in _tgsRepository.PlayerIdentity.AsQueryable()
                    where playerIdentity.PlayerCode == playerCode
                    join authForm in _tgsRepository.PlayerAuthenticationForms on playerIdentity.PlayerAuthenticationFormId equals authForm.Id
                    select new
                    {
                        authForm.Id,
                        authForm.Status
                    };

        var formData = query.FirstOrDefault();

        var status = formData == null
            ? PlayerIdentityReviewState.WaitingVerify
            : (PlayerIdentityReviewState)formData.Status;

        return ValueTask.FromResult(new PlayerIdentityFormInfo(formData?.Id, status));
    }

    public async ValueTask<PasswordInfo> GetPlayerLoginPasswordInfoAsync(string playerCode)
    {
        var playerData = await _tgsRepository.Players
            .FindAsync(playerCode)
            .ConfigureAwait(false);

        return new PasswordInfo(
            playerData!.Password,
            playerData!.Salt);
    }

    public async ValueTask PlayerLoginIpAsync(string playerCode, string ip, string host)
    {
        var data = _tgsRepository.PlayerIpRecords
            .AsQueryable()
            .Where(x => x.PlayerCode == playerCode && x.IpType == 2)
            .FirstOrDefault();

        if (data == null)
        {
            data = new PlayerIpRecord
            {
                PlayerCode = playerCode,
                IpType = 2,
                Ip = ip,
                Host = host,
                RecordOn = DateTimeOffset.UtcNow
            };

            _tgsRepository.PlayerIpRecords.Add(data);
        }
        else
        {
            data.Ip = ip;
            data.Host = host;
            data.RecordOn = DateTimeOffset.UtcNow;

            _tgsRepository.PlayerIpRecords.Update(data);
        }

        await _tgsRepository.SaveChangesAsync().ConfigureAwait(false);
    }

    public async ValueTask PlayerRegisterIpAsync(string playerCode, string ip, string? host, DateTimeOffset registeredOn)
    {
        var player = await _tgsRepository.Players
            .FindAsync(playerCode)
            .ConfigureAwait(false);

        player!.RegisteredOn = registeredOn;
        player!.UpdatedByUsername = playerCode;
        player!.UpdatedOn = DateTimeOffset.UtcNow;

        var registerIpRecord = new PlayerIpRecord
        {
            PlayerCode = playerCode,
            IpType = (int)IpType.RegisterIp,
            Ip = ip,
            Host = host,
            RecordOn = registeredOn
        };

        _ = await _tgsRepository.PlayerIpRecords
             .AddAsync(registerIpRecord)
             .ConfigureAwait(false);

        _ = await _tgsRepository
            .SaveChangesAsync()
            .ConfigureAwait(false);
    }

    public async ValueTask SetPlayerEmailAsync(string playerCode, string email)
    {
        var playerData = await _tgsRepository.Players
            .FindAsync(playerCode)
            .ConfigureAwait(false);

        if (playerData == null)
            return;

        playerData.Email = email;
        playerData.UpdatedOn = DateTimeOffset.UtcNow;

        _ = await _tgsRepository.SaveChangesAsync().ConfigureAwait(false);
    }

    public async ValueTask SetPlayerPhoneAsync(string playerCode, string areaCode, string phone)
    {
        var playerData = await _tgsRepository.Players
            .FindAsync(playerCode)
            .ConfigureAwait(false);

        if (playerData == null)
            return;

        playerData.AreaCode = areaCode;
        playerData.Phone = phone;
        playerData.PhoneAuthentication = true;

        playerData.UpdatedOn = DateTimeOffset.UtcNow;

        await _tgsRepository.SaveChangesAsync().ConfigureAwait(false);
    }

    public async ValueTask TogglePlayerAutoTransferFlagAsync(string playerCode)
    {
        var settingData = await _tgsRepository.PlayerGameSettings
            .FindAsync(playerCode)
            .ConfigureAwait(false);

        if (settingData == null)
        {
            settingData = new PlayerGameSetting
            {
                PlayerCode = playerCode,
                IsAutoTransferToGame = false
            };

            await _tgsRepository.PlayerGameSettings
                .AddAsync(settingData)
                .ConfigureAwait(false);
        }

        settingData.IsAutoTransferToGame = !settingData.IsAutoTransferToGame;

        _ = await _tgsRepository.SaveChangesAsync().ConfigureAwait(false);
    }

    public async ValueTask UpdatePlayerIdentityAsync(string playerCode, Guid identityAuthenticationFormId)
    {
        var identityData = await _tgsRepository.PlayerIdentity
            .FindAsync(playerCode)
            .ConfigureAwait(false);

        if (identityData == null)
        {
            identityData = new PlayerIdentity
            {
                PlayerCode = playerCode,
                PlayerAuthenticationFormId = identityAuthenticationFormId
            };

            _ = await _tgsRepository.PlayerIdentity
                .AddAsync(identityData)
                .ConfigureAwait(false);
        }
        else
        {
            identityData.PlayerAuthenticationFormId = identityAuthenticationFormId;
        }
        _ = await _tgsRepository
            .SaveChangesAsync()
            .ConfigureAwait(false);
    }

    public async ValueTask UpdatePlayerStatusAsync(string playerCode, PlayerStatus playerStatus)
    {
        var player = await _tgsRepository.Players.FindAsync(playerCode)
            .ConfigureAwait(false);

        player!.Status = playerStatus;
        player!.UpdatedOn = DateTimeOffset.UtcNow;

        _ = await _tgsRepository.SaveChangesAsync().ConfigureAwait(false);
    }
}
