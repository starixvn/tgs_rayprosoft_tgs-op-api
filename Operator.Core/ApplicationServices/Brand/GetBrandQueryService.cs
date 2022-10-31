using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.BrandCertificate.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Brand;

internal class GetBrandQueryService : IGetService<BrandQuery, BrandInfo>
{
	private readonly IBrandServiceClient _brandServiceClient;

	public GetBrandQueryService(IBrandServiceClient brandServiceClient)
	{
		_brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
	}

	public async ValueTask<BrandInfo> GetAsync(
		BrandQuery query,
		CancellationToken cancellationToken = default)
	{
		var brandInfo = await _brandServiceClient
			.GetBrandInfoAsync(query.BrandId, cancellationToken)
			.ConfigureAwait(false);

		if (brandInfo == null)
		{
			throw new BrandNotFoundException();
		}

		var frontendInfo = await _brandServiceClient
			.GetBrandFrontendInfoAsync(query.BrandId, cancellationToken)
			.ConfigureAwait(false)!;

		var status = await _brandServiceClient
			.GetBrandStatusAsync(query.BrandId, cancellationToken)
			.ConfigureAwait(false);

		return new BrandInfo(
			brandInfo.Id,
			brandInfo.Code,
			frontendInfo.DesktopLogoUrl,
			frontendInfo.LoadingUrl,
			frontendInfo.MobileLogoUrl,
			status,
			brandInfo.Currency,
			brandInfo.SmsEnabled,
			brandInfo.SmsRegisterFlowEnabled,
			brandInfo.PlayerIdentityEnabled,
			brandInfo.EmailRegisterFlowEnabled,
			brandInfo.BankInfoRegisterFlowEnabled,
			frontendInfo.PlayerRegisterFlowSettings);
	}
}
