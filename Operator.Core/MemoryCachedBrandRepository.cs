using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace TGS.Services.Operator;

internal class MemoryCachedBrandRepository(
	IMemoryCache memoryCache,
	IBrandRepository brandRepository) : IBrandRepository
{
	private readonly IBrandRepository _brandRepository = brandRepository ?? throw new ArgumentNullException(nameof(brandRepository));
	private readonly IMemoryCache _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

	public async ValueTask<string?> GetSecretKeyAsync(
		Guid brandId,
		CancellationToken cancellationToken = default)
	{
		var key = $"Brand:SecretKey:{brandId}";

		return await _memoryCache.GetOrCreateAsync(key, async entry =>
		{
			var secretKey = await _brandRepository.GetSecretKeyAsync(brandId, cancellationToken)
				.ConfigureAwait(false);

			entry.AbsoluteExpirationRelativeToNow = string.IsNullOrEmpty(secretKey)
				? TimeSpan.FromSeconds(10)
				: TimeSpan.FromDays(1);

			return secretKey;
		}).ConfigureAwait(false);
	}

	public IAsyncEnumerable<AnnouncementInfo> QueryAnnouncementInfosAsync(Guid brandId, DateTimeOffset searchDate)
		=> _brandRepository.QueryAnnouncementInfosAsync(brandId, searchDate);

	public IAsyncEnumerable<BannerInfo> QueryBrandBannersAsync(Guid brandId)
		=> _brandRepository.QueryBrandBannersAsync(brandId);

	public ValueTask<bool> CheckHaveAllAgentAsync(Guid brandId, CancellationToken cancellationToken = default)
		=> _brandRepository.CheckHaveAllAgentAsync(brandId, cancellationToken);

	public IAsyncEnumerable<BrandAllAgentTurnover> GetBrandAllAgentTurnoverConditionsAsync(Guid brandId, CancellationToken cancellationToken = default)
		=> _brandRepository.GetBrandAllAgentTurnoverConditionsAsync(brandId, cancellationToken);
}
