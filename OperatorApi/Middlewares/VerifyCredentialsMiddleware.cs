using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Tgs.BrandCertificate;
using Tgs.BrandCertificate.Clients;
using TGS.Core.Models.Messages;

namespace TGS.Apis.OperatorApi.Middlewares;

public sealed class VerifyCredentialsMiddleware(
	RequestDelegate next,
	ILogger<VerifyCredentialsMiddleware> logger)
{
	private static readonly ActivitySource _VerifyCredentialsMiddlewareSource = new($"TGS.Apis.OperatorApi.Middlewares.{nameof(VerifyCredentialsMiddleware)}");

	private readonly ILogger<VerifyCredentialsMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

	public async Task InvokeAsync(
		HttpContext context,
		IBrandServiceClient brandServiceClient)
	{
		using var activity = _VerifyCredentialsMiddlewareSource.StartActivity(nameof(VerifyCredentialsMiddleware));

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

		context.Items.Add(Core.Models.Consts.ContextItems.BrandId, brandId);

		await _next(context);
	}
}
