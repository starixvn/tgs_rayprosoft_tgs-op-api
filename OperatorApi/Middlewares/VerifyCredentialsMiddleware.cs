using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TGS.Core.Models;
using TGS.Core.Models.Exceptions;
using TGS.Services.Operations.Validators;

namespace TGS.Apis.OperatorApi.Middlewares;

public sealed class VerifyCredentialsMiddleware
{
    private static readonly ActivitySource _VerifyCredentialsMiddlewareSource = new($"TGS.Apis.OperatorApi.Middlewares.{nameof(VerifyCredentialsMiddleware)}");

    private readonly RequestDelegate _next;

    public VerifyCredentialsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IStatusValidator statusValidator)
    {
        using var activity = _VerifyCredentialsMiddlewareSource.StartActivity(nameof(VerifyCredentialsMiddleware));

        var brandId = context.GetBrandId();

        if (brandId is null)
            throw new MissingTokenException("Missing Authorization Header");

        await statusValidator.ThrowIfBrandStatusIsNotOneOfAsync(brandId.Value, ValidStatus.Default);

        context.Items.Add(Core.Models.Consts.ContextItems.BrandId, brandId);

        await _next(context);
    }
}
