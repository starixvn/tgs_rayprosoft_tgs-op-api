#nullable enable

using Microsoft.AspNetCore.Mvc.Filters;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Services.Operator.ApplicationServices.Player;

namespace TGS.Apis.OperatorApi.Filters;

public class PublishPlayerLoggedInEventFilter : IAsyncActionFilter
{
	public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
	{
		var executedContext = await next().ConfigureAwait(false);

		var playerTokenData = executedContext.HttpContext.Items.GetPlayerTokenData();

		if (playerTokenData is not null)
		{
			var commandService = executedContext.HttpContext.RequestServices
				.GetRequiredService<ICommandService<PublishPlayerLoggedInEventCommand>>();

			var command = new PublishPlayerLoggedInEventCommand(
				playerTokenData.PlayerCode,
				DateTimeOffset.UtcNow);

			await commandService.ExecuteAsync(command).ConfigureAwait(false);
		}
	}
}
