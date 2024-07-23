using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.BrandCertificate.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Brand;

internal class GetBrandQueryService(IBrandServiceClient brandServiceClient) : IGetService<BrandQuery, BrandInfo>
{
	private readonly IBrandServiceClient _brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));

	public async ValueTask<BrandInfo> GetAsync(
		BrandQuery query,
		CancellationToken cancellationToken = default)
	{
		var brandInfo = await _brandServiceClient
			.GetBrandInfoAsync(query.BrandId, cancellationToken)
			.ConfigureAwait(false) ?? throw new BrandNotFoundException();

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
			frontendInfo.MobileLogoUrl,
			frontendInfo.LoadingUrl,
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