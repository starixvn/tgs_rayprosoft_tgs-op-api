using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FeatureManagement;
using NSubstitute;
using NUnit.Framework.Legacy;
using Tgs.BrandCertificate;
using Tgs.BrandCertificate.Clients;
using Tgs.BrandGameCredits.Common;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Middlewares;
using TGS.Core.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Brand;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class VerifyBrandOperationMiddlewareTests
{
	[Test]
	public async Task 若HttpContext沒有BrandId時會回應StatusCode401()
	{
		var fakeRequestDelegate = Substitute.For<RequestDelegate>();

		var sut = new VerifyBrandOperationMiddleware(fakeRequestDelegate, NullLogger<VerifyBrandOperationMiddleware>.Instance);

		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeMaintenanceRedisRepository = Substitute.For<IMaintenanceRedisRepository>();
		var fakeBrandCreditQueryService = Substitute.For<IGetService<BrandCreditQuery, BrandGameCreditRiskTypes>>();
		var fakeFeatureManager = Substitute.For<IFeatureManager>();

		_ = fakeFeatureManager
			.IsEnabledAsync("BrandOperation")
			.Returns(false);

		var fakeHttpContext = new DefaultHttpContext();

		await sut.InvokeAsync(
			fakeHttpContext,
			fakeBrandServiceClient,
			fakeMaintenanceRedisRepository,
			fakeBrandCreditQueryService, fakeFeatureManager);

		_ = fakeBrandCreditQueryService
			.Received(0)
			.GetAsync(Arg.Any<BrandCreditQuery>(), Arg.Any<CancellationToken>());

		ClassicAssert.AreEqual(401, fakeHttpContext.Response.StatusCode);
	}

	[Test]
	public async Task 若BrandStatus不是開啟或維護時會回應StatusCode401()
	{
		var fakeRequestDelegate = Substitute.For<RequestDelegate>();

		var sut = new VerifyBrandOperationMiddleware(fakeRequestDelegate, NullLogger<VerifyBrandOperationMiddleware>.Instance);

		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeMaintenanceRedisRepository = Substitute.For<IMaintenanceRedisRepository>();
		var fakeBrandCreditQueryService = Substitute.For<IGetService<BrandCreditQuery, BrandGameCreditRiskTypes>>();
		var fakeFeatureManager = Substitute.For<IFeatureManager>();

		_ = fakeFeatureManager
			.IsEnabledAsync(Arg.Is("BrandOperation"))
			.Returns(false);

		var features = new FeatureCollection();

		var brandId = Guid.NewGuid();
		var fakeBrandFeature = Substitute.For<IBrandFeature>();
		_ = fakeBrandFeature.BrandId.Returns(brandId);
		features.Set(fakeBrandFeature);

		var fakeHttpResponse = Substitute.For<IHttpResponseFeature>();
		features.Set(fakeHttpResponse);

		var responseBodyStream = new MemoryStream();
		features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(responseBodyStream));

		var fakeHttpContext = new DefaultHttpContext(features);

		_ = fakeBrandServiceClient
			.GetBrandStatusAsync(brandId)
			.Returns(BrandStatus.Inactive);

		await sut.InvokeAsync(fakeHttpContext, fakeBrandServiceClient, fakeMaintenanceRedisRepository, fakeBrandCreditQueryService, fakeFeatureManager);

		_ = fakeBrandCreditQueryService
			.Received(0)
			.GetAsync(Arg.Any<BrandCreditQuery>(), Arg.Any<CancellationToken>());

		ClassicAssert.AreEqual(401, fakeHttpContext.Response.StatusCode);
		responseBodyStream.Position = 0;
		var body = JsonSerializer.Deserialize<TgsResponse>(new StreamReader(fakeHttpContext.Response.Body).ReadToEnd());

		ClassicAssert.AreEqual(404, body.Error);
		ClassicAssert.AreEqual("InvalidStatus", body.ErrorMessage);
		ClassicAssert.AreEqual("InvalidStatus", body.ErrorMessageKey);
	}

	[Test]
	public async Task 若平台已用額度為ModerateRisk且BrandOperation開關為true時會回應StatusCode503使玩家無法進入遊戲()
	{
		var fakeRequestDelegate = Substitute.For<RequestDelegate>();

		var sut = new VerifyBrandOperationMiddleware(fakeRequestDelegate, NullLogger<VerifyBrandOperationMiddleware>.Instance);

		var fakeMaintenanceRedisRepository = Substitute.For<IMaintenanceRedisRepository>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeBrandCreditQueryService = Substitute.For<IGetService<BrandCreditQuery, BrandGameCreditRiskTypes>>();
		var fakeFeatureManager = Substitute.For<IFeatureManager>();

		_ = fakeFeatureManager
			.IsEnabledAsync(Arg.Is("BrandOperation"))
			.Returns(true);

		var brandId = Guid.NewGuid();
		var features = new FeatureCollection();

		var fakeBrandFeature = Substitute.For<IBrandFeature>();
		_ = fakeBrandFeature.BrandId.Returns(brandId);
		features.Set(fakeBrandFeature);

		var fakeHttpRequest = Substitute.For<IHttpRequestFeature>();
		fakeHttpRequest.Path = "/api/game/geturl";
		features.Set(fakeHttpRequest);

		var fakeHttpResponse = Substitute.For<IHttpResponseFeature>();
		features.Set(fakeHttpResponse);

		var responseBodyStream = new MemoryStream();
		features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(responseBodyStream));

		var fakeHttpContext = new DefaultHttpContext(features);

		_ = fakeBrandServiceClient.GetBrandStatusAsync(brandId)
			.Returns(BrandStatus.Active);

		_ = fakeBrandCreditQueryService
			.GetAsync(Arg.Is(new BrandCreditQuery(brandId)), Arg.Any<CancellationToken>())
			.Returns(BrandGameCreditRiskTypes.ModerateRisk);

		await sut.InvokeAsync(fakeHttpContext, fakeBrandServiceClient, fakeMaintenanceRedisRepository, fakeBrandCreditQueryService, fakeFeatureManager);

		ClassicAssert.AreEqual(503, fakeHttpContext.Response.StatusCode);

		responseBodyStream.Position = 0;
		var body = JsonSerializer.Deserialize<TgsResponse>(new StreamReader(fakeHttpContext.Response.Body).ReadToEnd());

		ClassicAssert.AreEqual(403, body.Error);
		ClassicAssert.AreEqual("UnderMaintenance", body.ErrorMessage);
		ClassicAssert.AreEqual("UnderMaintenance", body.ErrorMessageKey);
	}

	[Test]
	public async Task 若平台已用額度為ModerateRisk且BrandOperation開關為false時則平台正常運作()
	{
		var fakeRequestDelegate = Substitute.For<RequestDelegate>();

		var sut = new VerifyBrandOperationMiddleware(fakeRequestDelegate, NullLogger<VerifyBrandOperationMiddleware>.Instance);

		var fakeMaintenanceRedisRepository = Substitute.For<IMaintenanceRedisRepository>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeBrandCreditQueryService = Substitute.For<IGetService<BrandCreditQuery, BrandGameCreditRiskTypes>>();
		var fakeFeatureManager = Substitute.For<IFeatureManager>();

		_ = fakeFeatureManager
			.IsEnabledAsync(Arg.Is("BrandOperation"))
			.Returns(false);

		var brandId = Guid.NewGuid();
		var features = new FeatureCollection();

		var fakeBrandFeature = Substitute.For<IBrandFeature>();
		_ = fakeBrandFeature.BrandId.Returns(brandId);
		features.Set(fakeBrandFeature);

		var fakeHttpRequest = Substitute.For<IHttpRequestFeature>();
		fakeHttpRequest.Path = "/api/game/geturl";
		features.Set(fakeHttpRequest);

		var fakeHttpResponse = Substitute.For<IHttpResponseFeature>();
		features.Set(fakeHttpResponse);

		var responseBodyStream = new MemoryStream();
		features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(responseBodyStream));

		var fakeHttpContext = new DefaultHttpContext(features);

		_ = fakeBrandServiceClient.GetBrandStatusAsync(brandId)
			.Returns(BrandStatus.Active);

		_ = fakeBrandCreditQueryService
			.GetAsync(Arg.Is(new BrandCreditQuery(brandId)), Arg.Any<CancellationToken>())
			.Returns(BrandGameCreditRiskTypes.ModerateRisk);

		await sut.InvokeAsync(fakeHttpContext, fakeBrandServiceClient, fakeMaintenanceRedisRepository, fakeBrandCreditQueryService, fakeFeatureManager);

		ClassicAssert.AreEqual(brandId, fakeHttpContext.Items["brandId"]);
	}

	[Test]
	public async Task 若平台切換至維護且BrandOperation開關為false時會回應StatusCode503使平台進入維護()
	{
		var fakeRequestDelegate = Substitute.For<RequestDelegate>();

		var sut = new VerifyBrandOperationMiddleware(fakeRequestDelegate, NullLogger<VerifyBrandOperationMiddleware>.Instance);

		var fakeMaintenanceRedisRepository = Substitute.For<IMaintenanceRedisRepository>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeBrandCreditQueryService = Substitute.For<IGetService<BrandCreditQuery, BrandGameCreditRiskTypes>>();
		var fakeFeatureManager = Substitute.For<IFeatureManager>();

		_ = fakeFeatureManager
			.IsEnabledAsync(Arg.Is("BrandOperation"))
			.Returns(false);

		var brandId = Guid.NewGuid();
		var features = new FeatureCollection();

		var fakeBrandFeature = Substitute.For<IBrandFeature>();
		_ = fakeBrandFeature.BrandId.Returns(brandId);
		features.Set(fakeBrandFeature);

		var fakeHttpResponse = Substitute.For<IHttpResponseFeature>();
		features.Set(fakeHttpResponse);

		var responseBodyStream = new MemoryStream();
		features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(responseBodyStream));

		var fakeHttpContext = new DefaultHttpContext(features);

		_ = fakeMaintenanceRedisRepository
			.GetBrandMaintenanceAsync(Arg.Is(brandId))
			.Returns($"Maintenance:Brands:{brandId}");

		await sut.InvokeAsync(fakeHttpContext, fakeBrandServiceClient, fakeMaintenanceRedisRepository, fakeBrandCreditQueryService, fakeFeatureManager);

		ClassicAssert.AreEqual(503, fakeHttpContext.Response.StatusCode);

		responseBodyStream.Position = 0;
		var body = JsonSerializer.Deserialize<TgsResponse>(new StreamReader(fakeHttpContext.Response.Body).ReadToEnd());

		ClassicAssert.AreEqual(403, body.Error);
		ClassicAssert.AreEqual("UnderMaintenance", body.ErrorMessage);
		ClassicAssert.AreEqual("UnderMaintenance", body.ErrorMessageKey);
	}

	[Test]
	public async Task 若平台切換至維護且BrandOperation開關為true時會回應StatusCode503使平台進入維護()
	{
		var fakeRequestDelegate = Substitute.For<RequestDelegate>();

		var sut = new VerifyBrandOperationMiddleware(fakeRequestDelegate, NullLogger<VerifyBrandOperationMiddleware>.Instance);

		var fakeMaintenanceRedisRepository = Substitute.For<IMaintenanceRedisRepository>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeBrandCreditQueryService = Substitute.For<IGetService<BrandCreditQuery, BrandGameCreditRiskTypes>>();
		var fakeFeatureManager = Substitute.For<IFeatureManager>();

		_ = fakeFeatureManager
			.IsEnabledAsync(Arg.Is("BrandOperation"))
			.Returns(true);

		var brandId = Guid.NewGuid();
		var features = new FeatureCollection();

		var fakeBrandFeature = Substitute.For<IBrandFeature>();
		_ = fakeBrandFeature.BrandId.Returns(brandId);
		features.Set(fakeBrandFeature);

		var fakeHttpResponse = Substitute.For<IHttpResponseFeature>();
		features.Set(fakeHttpResponse);

		var responseBodyStream = new MemoryStream();
		features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(responseBodyStream));

		var fakeHttpContext = new DefaultHttpContext(features);

		_ = fakeMaintenanceRedisRepository
			.GetBrandMaintenanceAsync(Arg.Is(brandId))
			.Returns($"Maintenance:Brands:{brandId}");

		await sut.InvokeAsync(fakeHttpContext, fakeBrandServiceClient, fakeMaintenanceRedisRepository, fakeBrandCreditQueryService, fakeFeatureManager);

		ClassicAssert.AreEqual(503, fakeHttpContext.Response.StatusCode);

		responseBodyStream.Position = 0;
		var body = JsonSerializer.Deserialize<TgsResponse>(new StreamReader(fakeHttpContext.Response.Body).ReadToEnd());

		ClassicAssert.AreEqual(403, body.Error);
		ClassicAssert.AreEqual("UnderMaintenance", body.ErrorMessage);
		ClassicAssert.AreEqual("UnderMaintenance", body.ErrorMessageKey);
	}

	[Test]
	public async Task 若平台已用額度為HighRisk且BrandOpertation開關為true時會回應StatusCode503使平台進入維護()
	{
		var fakeRequestDelegate = Substitute.For<RequestDelegate>();

		var sut = new VerifyBrandOperationMiddleware(fakeRequestDelegate, NullLogger<VerifyBrandOperationMiddleware>.Instance);

		var fakeMaintenanceRedisRepository = Substitute.For<IMaintenanceRedisRepository>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeBrandCreditQueryService = Substitute.For<IGetService<BrandCreditQuery, BrandGameCreditRiskTypes>>();
		var fakeFeatureManager = Substitute.For<IFeatureManager>();

		_ = fakeFeatureManager
			.IsEnabledAsync(Arg.Is("BrandOperation"))
			.Returns(true);

		var brandId = Guid.NewGuid();
		var features = new FeatureCollection();

		var fakeBrandFeature = Substitute.For<IBrandFeature>();
		_ = fakeBrandFeature.BrandId.Returns(brandId);
		features.Set(fakeBrandFeature);

		var fakeHttpResponse = Substitute.For<IHttpResponseFeature>();
		features.Set(fakeHttpResponse);

		var responseBodyStream = new MemoryStream();
		features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(responseBodyStream));

		var fakeHttpContext = new DefaultHttpContext(features);

		_ = fakeBrandCreditQueryService
			.GetAsync(Arg.Is(new BrandCreditQuery(brandId)), Arg.Any<CancellationToken>())
			.Returns(BrandGameCreditRiskTypes.HighRisk);

		await sut.InvokeAsync(fakeHttpContext, fakeBrandServiceClient, fakeMaintenanceRedisRepository, fakeBrandCreditQueryService, fakeFeatureManager);

		ClassicAssert.AreEqual(503, fakeHttpContext.Response.StatusCode);

		responseBodyStream.Position = 0;
		var body = JsonSerializer.Deserialize<TgsResponse>(new StreamReader(fakeHttpContext.Response.Body).ReadToEnd());

		ClassicAssert.AreEqual(403, body.Error);
		ClassicAssert.AreEqual("UnderMaintenance", body.ErrorMessage);
		ClassicAssert.AreEqual("UnderMaintenance", body.ErrorMessageKey);
	}

	[Test]
	public async Task 若平台已用額度為HighRisk且BrandOpertation開關為false時則平台正常運作()
	{
		var fakeRequestDelegate = Substitute.For<RequestDelegate>();

		var sut = new VerifyBrandOperationMiddleware(fakeRequestDelegate, NullLogger<VerifyBrandOperationMiddleware>.Instance);

		var fakeMaintenanceRedisRepository = Substitute.For<IMaintenanceRedisRepository>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeBrandCreditQueryService = Substitute.For<IGetService<BrandCreditQuery, BrandGameCreditRiskTypes>>();
		var fakeFeatureManager = Substitute.For<IFeatureManager>();

		_ = fakeFeatureManager
			.IsEnabledAsync(Arg.Is("BrandOperation"))
			.Returns(false);

		var brandId = Guid.NewGuid();
		var features = new FeatureCollection();

		var fakeBrandFeature = Substitute.For<IBrandFeature>();
		_ = fakeBrandFeature.BrandId.Returns(brandId);
		features.Set(fakeBrandFeature);

		var fakeHttpResponse = Substitute.For<IHttpResponseFeature>();
		features.Set(fakeHttpResponse);

		var responseBodyStream = new MemoryStream();
		features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(responseBodyStream));

		var fakeHttpContext = new DefaultHttpContext(features);

		_ = fakeBrandCreditQueryService
			.GetAsync(Arg.Is(new BrandCreditQuery(brandId)), Arg.Any<CancellationToken>())
			.Returns(BrandGameCreditRiskTypes.HighRisk);

		await sut.InvokeAsync(fakeHttpContext, fakeBrandServiceClient, fakeMaintenanceRedisRepository, fakeBrandCreditQueryService, fakeFeatureManager);

		ClassicAssert.AreEqual(brandId, fakeHttpContext.Items["brandId"]);
	}

	[Test]
	public async Task 若平台已用額度為LowRisk且BrandOpertation開關為false時平台可正常使用()
	{
		var fakeRequestDelegate = Substitute.For<RequestDelegate>();

		var sut = new VerifyBrandOperationMiddleware(fakeRequestDelegate, NullLogger<VerifyBrandOperationMiddleware>.Instance);

		var fakeMaintenanceRedisRepository = Substitute.For<IMaintenanceRedisRepository>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeBrandCreditQueryService = Substitute.For<IGetService<BrandCreditQuery, BrandGameCreditRiskTypes>>();
		var fakeFeatureManager = Substitute.For<IFeatureManager>();

		_ = fakeFeatureManager
			.IsEnabledAsync(Arg.Is("BrandOperation"))
			.Returns(false);

		var brandId = Guid.NewGuid();
		var features = new FeatureCollection();

		var fakeBrandFeature = Substitute.For<IBrandFeature>();
		_ = fakeBrandFeature.BrandId.Returns(brandId);
		features.Set(fakeBrandFeature);

		var fakeHttpResponse = Substitute.For<IHttpResponseFeature>();
		features.Set(fakeHttpResponse);

		var fakeHttpContext = new DefaultHttpContext(features);

		_ = fakeBrandCreditQueryService
			.GetAsync(Arg.Is(new BrandCreditQuery(brandId)), Arg.Any<CancellationToken>())
			.Returns(BrandGameCreditRiskTypes.LowRisk);

		await sut.InvokeAsync(fakeHttpContext, fakeBrandServiceClient, fakeMaintenanceRedisRepository, fakeBrandCreditQueryService, fakeFeatureManager);

		ClassicAssert.AreEqual(brandId, fakeHttpContext.Items["brandId"]);
	}

	[Test]
	public async Task 若平台已用額度為LowRisk且BrandOpertation開關為true時平台可正常使用()
	{
		var fakeRequestDelegate = Substitute.For<RequestDelegate>();

		var sut = new VerifyBrandOperationMiddleware(fakeRequestDelegate, NullLogger<VerifyBrandOperationMiddleware>.Instance);

		var fakeMaintenanceRedisRepository = Substitute.For<IMaintenanceRedisRepository>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeBrandCreditQueryService = Substitute.For<IGetService<BrandCreditQuery, BrandGameCreditRiskTypes>>();
		var fakeFeatureManager = Substitute.For<IFeatureManager>();

		_ = fakeFeatureManager
			.IsEnabledAsync(Arg.Is("BrandOperation"))
			.Returns(true);

		var brandId = Guid.NewGuid();
		var features = new FeatureCollection();

		var fakeBrandFeature = Substitute.For<IBrandFeature>();
		_ = fakeBrandFeature.BrandId.Returns(brandId);
		features.Set(fakeBrandFeature);

		var fakeHttpResponse = Substitute.For<IHttpResponseFeature>();
		features.Set(fakeHttpResponse);

		var fakeHttpContext = new DefaultHttpContext(features);

		_ = fakeBrandCreditQueryService
			.GetAsync(Arg.Is(new BrandCreditQuery(brandId)), Arg.Any<CancellationToken>())
			.Returns(BrandGameCreditRiskTypes.LowRisk);

		await sut.InvokeAsync(fakeHttpContext, fakeBrandServiceClient, fakeMaintenanceRedisRepository, fakeBrandCreditQueryService, fakeFeatureManager);

		ClassicAssert.AreEqual(brandId, fakeHttpContext.Items["brandId"]);
	}

	[Test]
	public async Task 若平台已用額度為NormalRisk且BrandOpertation開關為false時平台可正常使用()
	{
		var fakeRequestDelegate = Substitute.For<RequestDelegate>();

		var sut = new VerifyBrandOperationMiddleware(fakeRequestDelegate, NullLogger<VerifyBrandOperationMiddleware>.Instance);

		var fakeMaintenanceRedisRepository = Substitute.For<IMaintenanceRedisRepository>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeBrandCreditQueryService = Substitute.For<IGetService<BrandCreditQuery, BrandGameCreditRiskTypes>>();
		var fakeFeatureManager = Substitute.For<IFeatureManager>();

		_ = fakeFeatureManager
			.IsEnabledAsync(Arg.Is("BrandOperation"))
			.Returns(false);

		var brandId = Guid.NewGuid();
		var features = new FeatureCollection();

		var fakeBrandFeature = Substitute.For<IBrandFeature>();
		_ = fakeBrandFeature.BrandId.Returns(brandId);
		features.Set(fakeBrandFeature);

		var fakeHttpResponse = Substitute.For<IHttpResponseFeature>();
		features.Set(fakeHttpResponse);

		var fakeHttpContext = new DefaultHttpContext(features);

		_ = fakeBrandCreditQueryService
			.GetAsync(Arg.Is(new BrandCreditQuery(brandId)), Arg.Any<CancellationToken>())
			.Returns(BrandGameCreditRiskTypes.NormalRisk);

		await sut.InvokeAsync(fakeHttpContext, fakeBrandServiceClient, fakeMaintenanceRedisRepository, fakeBrandCreditQueryService, fakeFeatureManager);

		ClassicAssert.AreEqual(brandId, fakeHttpContext.Items["brandId"]);
	}

	[Test]
	public async Task 若平台已用額度為NormalRisk且BrandOpertation開關為true時平台可正常使用()
	{
		var fakeRequestDelegate = Substitute.For<RequestDelegate>();

		var sut = new VerifyBrandOperationMiddleware(fakeRequestDelegate, NullLogger<VerifyBrandOperationMiddleware>.Instance);

		var fakeMaintenanceRedisRepository = Substitute.For<IMaintenanceRedisRepository>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeBrandCreditQueryService = Substitute.For<IGetService<BrandCreditQuery, BrandGameCreditRiskTypes>>();
		var fakeFeatureManager = Substitute.For<IFeatureManager>();

		_ = fakeFeatureManager
			.IsEnabledAsync(Arg.Is("BrandOperation"))
			.Returns(true);

		var brandId = Guid.NewGuid();
		var features = new FeatureCollection();

		var fakeBrandFeature = Substitute.For<IBrandFeature>();
		_ = fakeBrandFeature.BrandId.Returns(brandId);
		features.Set(fakeBrandFeature);

		var fakeHttpResponse = Substitute.For<IHttpResponseFeature>();
		features.Set(fakeHttpResponse);

		var fakeHttpContext = new DefaultHttpContext(features);

		_ = fakeBrandCreditQueryService
			.GetAsync(Arg.Is(new BrandCreditQuery(brandId)), Arg.Any<CancellationToken>())
			.Returns(BrandGameCreditRiskTypes.NormalRisk);

		await sut.InvokeAsync(fakeHttpContext, fakeBrandServiceClient, fakeMaintenanceRedisRepository, fakeBrandCreditQueryService, fakeFeatureManager);

		ClassicAssert.AreEqual(brandId, fakeHttpContext.Items["brandId"]);
	}
}
