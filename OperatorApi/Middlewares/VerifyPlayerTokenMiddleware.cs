#nullable enable

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
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

		var brandId = context.GetBrandId();
		var token = context.Request.Headers.TryGetValue("Token", out var value) ? value.FirstOrDefault() : null;

		var playerTokenData = brandId is null || string.IsNullOrEmpty(token)
			? null
			: await playerTokenParserCommandService
				.GetAsync(new(brandId.Value, token), context.RequestAborted)
				.ConfigureAwait(false);

		try
		{
			if (playerTokenData is not null)
				await loginRecordValidateService.ExecuteAsync(
					new ValidatePlayerLoginRecordCommand(playerTokenData!.LoginRecordId, playerTokenData.PlayerCode),
					context.RequestAborted).ConfigureAwait(false);
		}
		catch (PlayerLoginExpiredException ex)
		{
			_logger.LogDebug(ex, "Player login expired.");

			playerTokenData = null;
		}

		if (playerTokenData is not null)
			context.Items.Add(Core.Models.Consts.ContextItems.PlayerToken, playerTokenData);

		var attribute = context.GetEndpoint()?.Metadata?.GetMetadata<AuthorizePlayerTokenAttribute>();

		if (attribute == null)
		{
			await _next(context).ConfigureAwait(false);
			return;
		}

		if ((!attribute.AllowNullOrEmpty || attribute.NeedAuthorize) && playerTokenData is null)
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

		await _next(context);
	}
}
