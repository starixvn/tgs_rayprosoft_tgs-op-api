using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework.Legacy;
using Tgs.BrandCertificate.Clients;
using Tgs.BrandGameCredits.Client;
using Tgs.BrandGameCredits.Common;
using Tgs.BrandGameCredits.Common.Models;
using TGS.Services.Operator.ApplicationServices.Brand;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class BrandCreditTests
{
	[Test]
	public async Task Get_Brand_Credit()
	{
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeBrandGameCreditsClient = Substitute.For<IBrandGameCreditsClient>();

		var sut = new BrandCreditQueryService(fakeBrandServiceClient, fakeBrandGameCreditsClient, NullLogger<BrandCreditQueryService>.Instance);

		var brandId = Guid.NewGuid();

		var brandInfo = new BrandInfo
		{
			Id = brandId,
			Code = "brandCode"
		};

		_ = fakeBrandServiceClient
			.GetBrandInfoAsync(brandId)
			.Returns(brandInfo);

		_ = fakeBrandGameCreditsClient
			.GameCreditGetCurrentAsync(Arg.Is(brandInfo.Code))
			.Returns(new BrandGameCredit(
				Guid.NewGuid(),
				"ResellerCode",
				Guid.NewGuid(),
				"LicenseeCode",
				brandId,
				brandInfo.Code,
				DateOnly.FromDateTime(DateTime.UtcNow),
				0M,
				0M,
				0M,
				0M,
				0M,
				0M,
				0M,
				0M,
				0M,
				CreditRiskType: BrandGameCreditRiskTypes.NormalRisk,
				DateTimeOffset.UtcNow,
				DateTimeOffset.UtcNow));

		var actual = await sut.GetAsync(new BrandCreditQuery(brandId));

		ClassicAssert.AreEqual(BrandGameCreditRiskTypes.NormalRisk, actual);
	}
}