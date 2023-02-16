#nullable enable

using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Tgs.Core.Infrastructures.Security;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
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
		IPlayerServiceClient playerServiceClient,
		IBrandRepository brandRepository,
		IJwtTokenHandler handler,
		IGetService<GetPlayerMainDataQuery, PlayerInfo?> playerDataQueryService,
		IGetService<PlayerTokenParserCommand, PlayerTokenData?> playerTokenParserCommandService,
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
				_logger.LogDebug("Missing Player Token");

				context.Response.StatusCode = StatusCodes.Status401Unauthorized;

				await context.Response.WriteAsJsonAsync(new TgsResponse
				{
					Error = 102,
					ErrorMessage = "InvalidOrExpiredPlayerToken",
					ErrorMessageKey = "InvalidOrExpiredPlayerToken"
				}).ConfigureAwait(false);

				return;
			}

			await _next(context);
			return;
		}

		var playerTokenData = await playerTokenParserCommandService
			.GetAsync(new(token))
			.ConfigureAwait(false);

		if (playerTokenData is null)
		{
			_logger.LogDebug("Cannot parse player token, maybe missing required token properties");

			context.Response.StatusCode = StatusCodes.Status401Unauthorized;

			await context.Response.WriteAsJsonAsync(new TgsResponse
			{
				Error = 102,
				ErrorMessage = "InvalidOrExpiredPlayerToken",
				ErrorMessageKey = "InvalidOrExpiredPlayerToken"
			}).ConfigureAwait(false);

			return;
		}
		
		context.Items.Add(Core.Models.Consts.ContextItems.PlayerToken, playerTokenData);

		try
		{
			await loginRecordValidateService.ExecuteAsync(
				new ValidatePlayerLoginRecordCommand(playerTokenData.LoginRecordId, playerTokenData.PlayerCode));
		}
		catch (PlayerLoginExpiredException ex)
		{
			_logger.LogDebug(ex, "Player login expired.");

			context.Response.StatusCode = StatusCodes.Status401Unauthorized;

			await context.Response.WriteAsJsonAsync(new TgsResponse
			{
				Error = 102,
				ErrorMessage = "InvalidOrExpiredPlayerToken",
				ErrorMessageKey = "InvalidOrExpiredPlayerToken"
			}).ConfigureAwait(false);

			return;
		}

		if (!attribute.NeedAuthorize)
		{
			await _next(context);
			return;
		}

		var brandId = context.GetBrandId();
		var secretKey = await brandRepository.GetSecretKeyAsync(brandId!.Value);

		var (isValid, _, _) = handler.ValidateToken(
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
			_ => _logger.LogDebug("{playerCode} jwt validate expired", playerTokenData.PlayerCode));

		if (isValid)
			await _next(context);
		else
		{
			context.Response.StatusCode = StatusCodes.Status401Unauthorized;

			await context.Response.WriteAsJsonAsync(new TgsResponse
			{
				Error = 102,
				ErrorMessage = "InvalidOrExpiredPlayerToken",
				ErrorMessageKey = "InvalidOrExpiredPlayerToken"
			});
		}
	}
}
