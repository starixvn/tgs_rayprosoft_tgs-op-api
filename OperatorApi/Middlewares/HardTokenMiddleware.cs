#nullable enable

using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Core.Models.Exceptions;
using TGS.Core.Models.Messages;
using TGS.Core.Models.Tokens;
using TGS.Services.Operator.ApplicationServices.Player;

namespace TGS.Apis.OperatorApi.Middlewares;

public sealed class HardTokenMiddleware(
	RequestDelegate next,
	ILogger<VerifyPlayerTokenMiddleware> logger)
{
	private static readonly ActivitySource _VerifyPlayerTokenMiddlewareSource = new($"TGS.Apis.OperatorApi.Middlewares.{nameof(VerifyPlayerTokenMiddleware)}");

	private readonly ILogger<VerifyPlayerTokenMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

	public async Task InvokeAsync(
		HttpContext context)
	{
		try
		{
			// get from https://global-api.nexentgame.com/global-api/brand/token/TGS
			var token = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1lIjoiVEdTIiwic3ViIjoiZDcxYWM1ZWQtYjdkOS00OWYyLWExMjktODcwOWZhNGNjMDJhIiwibmJmIjoxNzIzNzk2MjQ1LCJleHAiOjE3MjM3OTk4NDUsImlhdCI6MTcyMzc5NjI0NSwiaXNzIjoidGdzIn0.Tnu2SNJimeP2sAZgb2VldriTcJ16KR27Lsl1H10Yrzwx_eifLga3xzhnNAd8-2skgv5K9ZW6UHEe0UIjz_t_0ek1jmKPMyjhFAQCg5gUUTbNzqBlnGHp89p_6TU6q4LudOaOL1ioBvtbRbq1WVX5DbiFz5VaqFORNmVoPz7JZHJLG4Ra1vK8crFSr5sPEs4Lb3dgarfsX5wiM1R_xxEovTaWr81fUlhqDrT951adyUSrvK1oby8JBdq8w6g2m5AlXKP1aI3FA3zy54ys6pBC3lwUCY9RS_iEeqjUlBDzaXL8LX7yhaQLf2VZLKi5XUD1vnLZIgZ3J4eZugFluN2tDQ";
			context.Request.Headers.TryAdd("brand-token", token);
		}
		finally
		{
			await _next(context).ConfigureAwait(false);
		}
	}
}