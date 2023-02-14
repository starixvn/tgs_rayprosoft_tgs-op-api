#nullable enable

using Microsoft.AspNetCore.Mvc;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Services.Operator.ApplicationServices.Player;
using Tgs.ServiceUtility;

namespace TGS.Apis.OperatorApi.Middlewares;

public class PublishPlayerLoggedInEventMiddleware
{
	private readonly RequestDelegate _next;

	public PublishPlayerLoggedInEventMiddleware(
		RequestDelegate next)
	{
		_next = next ?? throw new ArgumentNullException(nameof(next));
	}

	public async Task InvokeAsync(
		HttpContext context,
		ICommandService<PublishPlayerLoggedInEventCommand> commandService)
	{
		await _next(context);

		var playerTokenData = context.Items.GetPlayerTokenData();

		if (playerTokenData is not null)
		{
			var command = new PublishPlayerLoggedInEventCommand(
				playerTokenData.PlayerCode,
				DateTimeOffset.UtcNow);

			await commandService.ExecuteAsync(command);
		}
	}
}
