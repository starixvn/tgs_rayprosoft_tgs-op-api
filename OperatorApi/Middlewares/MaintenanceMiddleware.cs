using System.Diagnostics;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Core.Models.Messages;
using TGS.Services.Operator;

namespace TGS.Apis.OperatorApi.Middlewares;

public sealed class MaintenanceMiddleware
{
	private static readonly ActivitySource _MaintenanceMiddlewareSource = new($"TGS.Apis.OperatorApi.Middlewares.{nameof(MaintenanceMiddleware)}");
	private readonly RequestDelegate _next;

	public MaintenanceMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	public async Task InvokeAsync(HttpContext context,
	   IMaintenanceRedisRepository maintenanceRedisService)
	{
		using var activity = _MaintenanceMiddlewareSource.StartActivity(nameof(MaintenanceMiddleware));

		var brandId = context.Items.GetBrandId();

		if (!string.IsNullOrEmpty(await maintenanceRedisService.GetBrandMaintenanceAsync(brandId).ConfigureAwait(false)))
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

		await _next(context);
	}
}
