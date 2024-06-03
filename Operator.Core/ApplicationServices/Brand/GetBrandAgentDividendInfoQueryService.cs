using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Tgs.BrandCertificate.Clients;
using TGS.Core.Models.Enums;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Brand;

internal class GetBrandAgentDividendInfoQueryService(IBrandServiceClient brandServiceClient, IMemoryCache memoryCache)
	: IGetService<GetBrandAgentDividendInfoQuery, BrandAgentDividendInfo?>
{
	public async ValueTask<BrandAgentDividendInfo?> GetAsync(
		GetBrandAgentDividendInfoQuery query,
		CancellationToken cancellationToken = default)
	{
		var key = $"BrandAgentDividendInfo:{query.BrandId}";

		return await memoryCache.GetOrCreateAsync(
				key,
				async entry =>
				{
					entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5D);

					var brandInfo = await brandServiceClient
						                .GetBrandFullInfoAsync(query.BrandId, cancellationToken)
						                .ConfigureAwait(false) ??
					                throw new BrandNotFoundException();

					return new BrandAgentDividendInfo(brandInfo.Id, brandInfo.AgentDividendType);
				})
			.ConfigureAwait(false);
	}
}