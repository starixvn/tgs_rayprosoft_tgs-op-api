using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tgs.BrandCertificate;
using Tgs.BrandCertificate.Clients;
using TGS.Core.Models.Exceptions;

namespace TGS.Apis.OperatorApi.Middlewares;

public sealed class VerifyCredentialsMiddleware
{
    private static readonly ActivitySource _VerifyCredentialsMiddlewareSource = new($"TGS.Apis.OperatorApi.Middlewares.{nameof(VerifyCredentialsMiddleware)}");

    private readonly RequestDelegate _next;

    public VerifyCredentialsMiddleware(
        RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(
        HttpContext context,
        IBrandServiceClient brandServiceClient)
    {
        using var activity = _VerifyCredentialsMiddlewareSource.StartActivity(nameof(VerifyCredentialsMiddleware));

        var brandId = context.GetBrandId();

        if (brandId is null)
            throw new MissingTokenException("Missing Authorization Header");

        var brandStatus = await brandServiceClient.GetBrandStatusAsync(brandId.Value);

        if (brandStatus is not (BrandStatus.Active or BrandStatus.Maintenance))
        {
            throw new InvalidStatusException("Brand is not active");
        }

        context.Items.Add(Core.Models.Consts.ContextItems.BrandId, brandId);

        await _next(context);
    }
}
