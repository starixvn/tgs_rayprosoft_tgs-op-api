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
		var playerTokenData = context.Items.GetPlayerTokenData();

		if (playerTokenData is not null)
		{
			var brandId = context.GetBrandId();

			var command = new PublishPlayerLoggedInEventCommand(
				playerTokenData.PlayerCode,
				brandId!.Value,
				DateTimeOffset.UtcNow);

			await commandService.ExecuteAsync(command);
		}

		await _next(context);
	}
}
