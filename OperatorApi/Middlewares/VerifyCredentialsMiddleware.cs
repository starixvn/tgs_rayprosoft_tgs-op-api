using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Tgs.BrandCertificate;
using Tgs.BrandCertificate.Clients;
using TGS.Core.Models.Messages;

namespace TGS.Apis.OperatorApi.Middlewares;

public sealed class VerifyCredentialsMiddleware
{
    private static readonly ActivitySource _VerifyCredentialsMiddlewareSource = new($"TGS.Apis.OperatorApi.Middlewares.{nameof(VerifyCredentialsMiddleware)}");

    private readonly ILogger<VerifyCredentialsMiddleware> _logger;
    private readonly RequestDelegate _next;

    public VerifyCredentialsMiddleware(
        RequestDelegate next,
        ILogger<VerifyCredentialsMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
