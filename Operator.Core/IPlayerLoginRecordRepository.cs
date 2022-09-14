using System;
using System.Threading.Tasks;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator;

public interface IPlayerLoginRecordRepository
{
    ValueTask<LoginRecordInfo> AddPlayerLoginRecordAsync(Guid brandId, string playerCode, PlatformType platformType, string ipAddress);

    ValueTask DeauthorizePlayerAsync(Guid loginRecordId);

    ValueTask<LoginRecordInfo?> GetPlayerLoginRecordAsync(Guid loginRecordId);
}
