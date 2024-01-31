using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tgs.BrandCertificate.Clients;
using Tgs.BrandGameCredits.Client;
using Tgs.BrandGameCredits.Common;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Brand;

internal class BrandCreditQueryService(
	IBrandServiceClient brandServiceClient,
	IBrandGameCreditsClient creditsClient,
	ILogger<BrandCreditQueryService> logger) : IGetService<BrandCreditQuery, BrandGameCreditRiskTypes>
{
	public async ValueTask<BrandGameCreditRiskTypes> GetAsync(BrandCreditQuery query, CancellationToken cancellationToken = default)
	{
		var brandInfo = await brandServiceClient
			.GetBrandInfoAsync(query.BrandId, cancellationToken)
			.ConfigureAwait(false)
			?? throw new InvalidOperationException($"Brand not found with id: {query.BrandId}");

		var brandGameCredit = await creditsClient
			.GameCreditGetCurrentAsync(brandInfo.Code, cancellationToken)
			.ConfigureAwait(false);

		if (brandGameCredit is null)
		{
			logger.LogWarning("{brandCode} brand Game Credit is null", brandInfo.Code);
			throw new Exception("Brand Game Credit is null");
		}

		logger.LogInformation("{brandCode} Credit Risk Type is: {riskType}", brandInfo.Code, brandGameCredit.CreditRiskType);

		return brandGameCredit!.CreditRiskType;
	}
}
