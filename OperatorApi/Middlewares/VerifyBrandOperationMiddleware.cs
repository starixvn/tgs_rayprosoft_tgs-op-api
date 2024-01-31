using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Tgs.BrandCertificate;
using Tgs.BrandCertificate.Clients;
using Tgs.BrandGameCredits.Common;
using Tgs.ServiceUtility;
using TGS.Core.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Brand;

namespace TGS.Apis.OperatorApi.Middlewares;

public sealed class VerifyBrandOperationMiddleware(
	RequestDelegate next,
	ILogger<VerifyBrandOperationMiddleware> logger)
{
	private static readonly ActivitySource _VerifyCredentialsMiddlewareSource = new($"TGS.Apis.OperatorApi.Middlewares.{nameof(VerifyBrandOperationMiddleware)}");

	private readonly ILogger<VerifyBrandOperationMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

	public async Task InvokeAsync(
		HttpContext context,
		IBrandServiceClient brandServiceClient,
		IMaintenanceRedisRepository maintenanceRedisService,
		IGetService<BrandCreditQuery, BrandGameCreditRiskTypes> brandCreditService,
		IFeatureManager featureManager)
	{
		using var activity = _VerifyCredentialsMiddlewareSource.StartActivity(nameof(VerifyBrandOperationMiddleware));

		var brandId = context.GetBrandId();

		if (brandId is null)
		{
			_logger.LogDebug("Missing Brand Token");

			context.Response.StatusCode = StatusCodes.Status401Unauthorized;

			await context.Response.WriteAsJsonAsync(new TgsResponse
			{
				Error = 101,
				ErrorMessage = "InvalidCredentials",
				ErrorMessageKey = "InvalidCredentials"
			}).ConfigureAwait(false);

			return;
		}

		var brandStatus = await brandServiceClient.GetBrandStatusAsync(brandId.Value);

		if (brandStatus is not (BrandStatus.Active or BrandStatus.Maintenance))
		{
			_logger.LogDebug("Brand is inactive");

			context.Response.StatusCode = StatusCodes.Status401Unauthorized;

			await context.Response.WriteAsJsonAsync(new TgsResponse
			{
				Error = 404,
				ErrorMessage = "InvalidStatus",
				ErrorMessageKey = "InvalidStatus"
			}).ConfigureAwait(false);

			return;
		}


		var brandRiskType = await brandCreditService
			.GetAsync(new BrandCreditQuery(brandId.Value))
			.ConfigureAwait(false);

		var brandOperationSwitch = await featureManager
			.IsEnabledAsync("BrandOperation")
			.ConfigureAwait(false);

		if (brandOperationSwitch is true && brandRiskType is BrandGameCreditRiskTypes.ModerateRisk)
		{
			var openGameApiUrls = new PathString[]
			{
				new("/api/game/lobby/geturl"),
				new("/api/game/geturl"),
				new("/api/game/sm01"),
				new("/api/game/bc")
			};
			if (openGameApiUrls.Contains(context.Request.Path))
			{
				context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

				await context.Response.WriteAsJsonAsync(new TgsResponse
				{
					Error = 403,
					ErrorMessage = "UnderMaintenance",
					ErrorMessageKey = "UnderMaintenance"
				}).ConfigureAwait(false);

				return;
			}
		}

		var hasBrandMaintenanceRedisCache = !string.IsNullOrEmpty(await maintenanceRedisService
			.GetBrandMaintenanceAsync(brandId.Value)
			.ConfigureAwait(false));

		if (hasBrandMaintenanceRedisCache is true
			|| (brandOperationSwitch is true && brandRiskType is BrandGameCreditRiskTypes.HighRisk))
		{
			_logger.LogDebug("Has Brand Maintenance Redis Cache: {hasBrandMaintenanceRedisCache}", hasBrandMaintenanceRedisCache);

			context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

			await context.Response.WriteAsJsonAsync(new TgsResponse
			{
				Error = 403,
				ErrorMessage = "UnderMaintenance",
				ErrorMessageKey = "UnderMaintenance"
			}).ConfigureAwait(false);

			return;
		}

		context.Items.Add(Core.Models.Consts.ContextItems.BrandId, brandId);

		await _next(context);
	}
}
