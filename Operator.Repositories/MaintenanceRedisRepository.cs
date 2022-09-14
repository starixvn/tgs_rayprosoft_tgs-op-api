using System;
using System.Threading.Tasks;
using TGS.Core.Infrastructures.Redis;
using TGS.Services.Operator;

namespace TGS.Operator.Repositories;

public sealed class MaintenanceRedisRepository : IMaintenanceRedisRepository
{
    private const string _brands = "Brands";
    private const string _licensees = "Licensees";
    private const string _maintenance = "Maintenance";
    private const string _resellers = "Resellers";
    private readonly IRedisCacheManager _cacheManager;

    public MaintenanceRedisRepository(IRedisCacheManager cacheManager)
    {
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
    }

    public ValueTask<string?> GetBrandMaintenanceAsync(Guid id)
        => GetMaintenanceAsync(_brands, id);

    public ValueTask<string?> GetLicenseeMaintenanceAsync(Guid id)
        => GetMaintenanceAsync(_licensees, id);

    public ValueTask<string?> GetResellerMaintenanceAsync(Guid id)
                        => GetMaintenanceAsync(_resellers, id);

    private async ValueTask<string?> GetMaintenanceAsync(string merchantKey, Guid id)
    {
        string key = $"{merchantKey }:{id}";
        return await _cacheManager
            .SafeGetAsync(_maintenance, key, () => Task.FromResult(default(string)))
            .ConfigureAwait(false);
    }
}
