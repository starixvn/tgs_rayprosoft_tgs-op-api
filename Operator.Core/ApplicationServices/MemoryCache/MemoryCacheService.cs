using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace TGS.Services.Operator.ApplicationServices.MemoryCache;

public interface IMemoryCacheService
{
	Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, T defaultValue);
}
public class MemoryCacheService(IMemoryCache memoryCache, IConfiguration configuration) : IMemoryCacheService
{
	private readonly IMemoryCache _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

	public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, T defaultValue)
	{
		if (!configuration.GetValue("CacheConfig:Enable", false))
			return await factory();

		var value = await _memoryCache.GetOrCreateAsync<T>(key, async entry =>
		{
			entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(configuration.GetValue<int>("CacheConfig:ExpirationTimeByMinutes"));
			return await factory();
		}).ConfigureAwait(false);
		return value ?? defaultValue;

	}
}
