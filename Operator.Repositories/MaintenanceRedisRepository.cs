using System;
using System.Threading.Tasks;
using TGS.Core.Infrastructures.Redis;
using TGS.Services.Operator;

namespace TGS.Operator.Repositories;

public sealed class MaintenanceRedisRepository(IRedisCacheManager cacheManager) : IMaintenanceRedisRepository
{
	private const string Brands = "Brands";
	private const string Licensees = "Licensees";
	private const string Maintenance = "Maintenance";
	private const string Resellers = "Resellers";
	private readonly IRedisCacheManager _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));

	public ValueTask<string?> GetBrandMaintenanceAsync(Guid id)
		=> GetMaintenanceAsync(Brands, id);

	public ValueTask<string?> GetLicenseeMaintenanceAsync(Guid id)
		=> GetMaintenanceAsync(Licensees, id);

	public ValueTask<string?> GetResellerMaintenanceAsync(Guid id)
		=> GetMaintenanceAsync(Resellers, id);

	private async ValueTask<string?> GetMaintenanceAsync(string merchantKey, Guid id)
	{
		string key = $"{merchantKey}:{id}";
		return await _cacheManager
			.SafeGetAsync(Maintenance, key, () => Task.FromResult(default(string)))
			.ConfigureAwait(false);
	}
}
