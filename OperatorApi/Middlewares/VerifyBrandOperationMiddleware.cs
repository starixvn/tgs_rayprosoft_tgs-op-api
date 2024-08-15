using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
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
	ILogger<VerifyBrandOperationMiddleware> logger,
	IConfiguration configuration)
{
	private static readonly ActivitySource _VerifyCredentialsMiddlewareSource = new(
		$"TGS.Apis.OperatorApi.Middlewares.{nameof(VerifyBrandOperationMiddleware)}");

	private static readonly PathString[] _OpenGameApiUrls = [
		new("/api/game/lobby/geturl"),
		new("/api/game/geturl"),
		new("/api/game/sm01"),
		new("/api/game/bc")];

	public async Task InvokeAsync(
		HttpContext context,
		IBrandServiceClient brandServiceClient,
		IMaintenanceRedisRepository maintenanceRedisService,
		IGetService<BrandCreditQuery, BrandGameCreditRiskTypes> brandCreditService)
	{
		using var activity = _VerifyCredentialsMiddlewareSource.StartActivity(nameof(VerifyBrandOperationMiddleware));

		var brandId = context.GetBrandId();

		if (!await CheckBrandIdNotNullAsync(
			context,
			brandId).ConfigureAwait(false))
			return;

		if (!await CheckBrandStatusAsync(
			context,
			brandId.Value,
			brandServiceClient).ConfigureAwait(false))
			return;

		if (!await CheckRiskAsync(
			context,
			brandId.Value,
			brandServiceClient,
			brandCreditService).ConfigureAwait(false))
			return;

		if (!await CheckMaintenanceAsync(
			context,
			brandId.Value,
			maintenanceRedisService).ConfigureAwait(false))
			return;

		context.Items.Add(Core.Models.Consts.ContextItems.BrandId, brandId);

		await next(context);
	}

	private static async ValueTask<bool> CheckRiskAsync(
		HttpContext context,
		Guid brandId,
		IBrandServiceClient brandServiceClient,
		IGetService<BrandCreditQuery, BrandGameCreditRiskTypes> brandCreditService)
	{
		var checkPlatformAccountFundIsEnabled = await brandServiceClient.GetBrandPlatformAccountFundAsync(
			brandId,
			context.RequestAborted).ConfigureAwait(false)
			?? false;

		if (!checkPlatformAccountFundIsEnabled)
			return true;

		var brandRiskType = await brandCreditService.GetAsync(
			new BrandCreditQuery(brandId),
			context.RequestAborted).ConfigureAwait(false);

		if (brandRiskType is BrandGameCreditRiskTypes.ModerateRisk)
		{
			if (_OpenGameApiUrls.Contains(context.Request.Path))
			{
				context.Response.StatusCode = StatusCodes.Status402PaymentRequired;

				await context.Response.WriteAsJsonAsync(new TgsResponse
				{
					Error = 403,
					ErrorMessage = "UnderMaintenance",
					ErrorMessageKey = "GameMaintenance"
				}).ConfigureAwait(false);

				return false;
			}
		}

		if (brandRiskType is BrandGameCreditRiskTypes.HighRisk)
		{
			context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

			await context.Response.WriteAsJsonAsync(new TgsResponse
			{
				Error = 403,
				ErrorMessage = "UnderMaintenance",
				ErrorMessageKey = "UnderMaintenance"
			}).ConfigureAwait(false);

			return false;
		}

		return true;
	}

	private async ValueTask<bool> CheckBrandIdNotNullAsync(HttpContext context, Guid? brandId)
	{
		if (brandId is null)
		{
			logger.LogDebug("Missing Brand Token");

			context.Response.StatusCode = StatusCodes.Status401Unauthorized;

			await context.Response.WriteAsJsonAsync(new TgsResponse
			{
				Error = 101,
				ErrorMessage = "InvalidCredentials",
				ErrorMessageKey = "InvalidCredentials"
			}).ConfigureAwait(false);

			return false;
		}

		return true;
	}

	private async ValueTask<bool> CheckBrandStatusAsync(
		HttpContext context,
		Guid brandId,
		IBrandServiceClient brandServiceClient)
	{
		var brandStatus = await brandServiceClient.GetBrandStatusAsync(
			brandId,
			context.RequestAborted).ConfigureAwait(false);

		if (brandStatus is not (BrandStatus.Active or BrandStatus.Maintenance))
		{
			logger.LogDebug("Brand is inactive");

			context.Response.StatusCode = StatusCodes.Status401Unauthorized;

			await context.Response.WriteAsJsonAsync(new TgsResponse
			{
				Error = 404,
				ErrorMessage = "InvalidStatus",
				ErrorMessageKey = "InvalidStatus"
			}).ConfigureAwait(false);

			return false;
		}

		return true;
	}

	private async ValueTask<bool> CheckMaintenanceAsync(
		HttpContext context,
		Guid brandId,
		IMaintenanceRedisRepository maintenanceRedisService)
	{
		var hasBrandMaintenanceRedisCache = !string.IsNullOrEmpty(await maintenanceRedisService
			.GetBrandMaintenanceAsync(brandId)
			.ConfigureAwait(false));

		if (hasBrandMaintenanceRedisCache is true)
		{
			logger.LogDebug("Has Brand Maintenance Redis Cache: {hasBrandMaintenanceRedisCache}", hasBrandMaintenanceRedisCache);

			context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

			await context.Response.WriteAsJsonAsync(new TgsResponse
			{
				Error = 403,
				ErrorMessage = "UnderMaintenance",
				ErrorMessageKey = "UnderMaintenance"
			}).ConfigureAwait(false);

			return false;
		}

		return true;
	}
}