#nullable enable

using Microsoft.AspNetCore.Mvc.Filters;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Services.Operator.ApplicationServices.Player;

namespace TGS.Apis.OperatorApi.Filters;

public class PublishPlayerLoggedInEventFilter(ILogger<PublishPlayerLoggedInEventFilter> logger) : IAsyncActionFilter
{
	private readonly ILogger<PublishPlayerLoggedInEventFilter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
	{
		var executedContext = await next().ConfigureAwait(false);

		var playerTokenData = executedContext.HttpContext.Items.GetPlayerTokenData();

		if (playerTokenData is not null)
		{
			var httpContext = executedContext.HttpContext;
			var commandService = httpContext.RequestServices
				.GetRequiredService<ICommandService<PublishPlayerLoggedInEventCommand>>();

			var command = new PublishPlayerLoggedInEventCommand(
				playerTokenData.PlayerCode,
				DateTimeOffset.UtcNow);

			try
			{
				await commandService.ExecuteAsync(
					command,
					httpContext.RequestAborted).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				_logger.LogInformation("Request canceled.");
			}
		}
	}
}