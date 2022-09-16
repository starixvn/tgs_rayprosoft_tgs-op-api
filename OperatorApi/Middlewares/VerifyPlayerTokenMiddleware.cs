#nullable enable

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Tgs.Core.Infrastructures.Security;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Resources;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Exceptions;
using TGS.Core.Models.Messages;
using TGS.Core.Models.Tokens;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Player;
using PlayerInfo = Tgs.PlayerService.Client.PlayerInfo;

namespace TGS.Apis.OperatorApi.Middlewares;

public sealed class VerifyPlayerTokenMiddleware
{
    private static readonly ActivitySource _VerifyPlayerTokenMiddlewareSource = new($"TGS.Apis.OperatorApi.Middlewares.{nameof(VerifyPlayerTokenMiddleware)}");

    private readonly ILogger<VerifyPlayerTokenMiddleware> _logger;
    private readonly RequestDelegate _next;

    public VerifyPlayerTokenMiddleware(
        RequestDelegate next,
        ILogger<VerifyPlayerTokenMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITokenProvider tokenProvider,
        IHashProvider hashProvider,
        IPlayerServiceClient playerServiceClient,
        IBrandRepository brandRepository,
        IJwtTokenHandler handler,
        IGetService<GetPlayerMainDataQuery, PlayerInfo?> playerDataQueryService,
        ICommandService<ValidatePlayerLoginRecordCommand> loginRecordValidateService)
    {
        using var activity = _VerifyPlayerTokenMiddlewareSource.StartActivity(nameof(VerifyPlayerTokenMiddleware));

        var attribute = context.GetEndpoint()?.Metadata?.GetMetadata<AuthorizePlayerTokenAttribute>();

        if (attribute == null)
        {
            await _next(context);
            return;
        }

        var token = context.Request.Headers.TryGetValue("Token", out var value) ? value.FirstOrDefault() : null;

        if (string.IsNullOrEmpty(token))
        {
            if (!attribute.AllowNullOrEmpty)
            {
                throw new MissingTokenException(Player.MissingPlayerToken);
            }

            await _next(context);
            return;
        }

        var jwt = handler.ReadToken(token);

        var claims = jwt.Claims.ToDictionary(c => c.Type, c => c.Value);

        if (!(claims.TryGetValue("sub", out var playerCode) && !string.IsNullOrEmpty(playerCode))
            || !(claims.TryGetValue("login_id", out var loginId) && !string.IsNullOrEmpty(loginId))
            || !(claims.TryGetValue("platform_type", out var platform) && !string.IsNullOrEmpty(platform))
            || !(claims.TryGetValue("exp", out var expire) && !string.IsNullOrEmpty(expire)))
        {
            throw new MissingTokenException(Player.MissingPlayerToken);
        }

        var playerTokenData = new PlayerTokenData
        {
            PlayerCode = playerCode,
            LoginRecordId = Guid.Parse(loginId),
            PlatformType = Enum.Parse<PlatformType>(platform),
            ExpiredOn = new DateTimeOffset(EpochTime.DateTime(long.Parse(expire)))
        };

        context.Items.Add(Core.Models.Consts.ContextItems.PlayerToken, playerTokenData);

        await loginRecordValidateService.ExecuteAsync(
            new ValidatePlayerLoginRecordCommand(playerTokenData.LoginRecordId, playerCode));

        if (!attribute.NeedAuthorize)
        {
            await _next(context);
            return;
        }

        var brandId = context.GetBrandId();
        var secretKey = await brandRepository.GetSecretKeyAsync(brandId!.Value);

        var result = handler.ValidateToken(
            token,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Consts.Site,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            },
            exception => _logger.LogDebug(exception, "Token validation failed"),
            _ => _logger.LogDebug("{playerCode} jwt validate expired", playerCode));

        if (result.IsValid)
            await _next(context);
        else
        {
            await context.Response.WriteAsJsonAsync(new TgsResponse
            {
                Error = 102,
                ErrorMessage = "InvalidOrExpiredPlayerToken",
                ErrorMessageKey = "InvalidOrExpiredPlayerToken"
            }).ConfigureAwait(false);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
    }
}
