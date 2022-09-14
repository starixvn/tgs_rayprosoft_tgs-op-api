using System;
using System.Threading.Tasks;

namespace TGS.Services.Operator;

public interface IMaintenanceRedisRepository
{
    ValueTask<string?> GetBrandMaintenanceAsync(Guid id);

    ValueTask<string?> GetLicenseeMaintenanceAsync(Guid id);

    ValueTask<string?> GetResellerMaintenanceAsync(Guid ids);
}
