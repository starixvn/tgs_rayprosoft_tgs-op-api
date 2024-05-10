using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Tgs.BrandCertificate.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Brand;

internal class GetPlatformChargeQueryService(IBrandServiceClient brandServiceClient, IMemoryCache memoryCache)
	: IGetService<GetPlatformChargeQuery, PlatformChargeInfo?>
{
	public async ValueTask<PlatformChargeInfo?> GetAsync(
		GetPlatformChargeQuery query,
		CancellationToken cancellationToken = default)
	{
		var key = $"PlatformChargeInfo:{query.BrandId}";

		return await memoryCache.GetOrCreateAsync(
				key,
				async entry =>
				{
					entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5D);

					var brandInfo = await brandServiceClient
						                .GetBrandFullInfoAsync(query.BrandId, cancellationToken)
						                .ConfigureAwait(false) ??
					                throw new BrandNotFoundException();

					return new PlatformChargeInfo(brandInfo.Id, brandInfo.PlatformCharge);
				})
			.ConfigureAwait(false);
	}
}