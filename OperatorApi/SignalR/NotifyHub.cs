#nullable enable

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TGS.Apis.OperatorApi.Extensions;

namespace TGS.Apis.OperatorApi.SignalR;

public class NotifyHub : Hub
{
	private readonly ILogger<NotifyHub> _logger;

	public NotifyHub(ILogger<NotifyHub> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public override async Task OnConnectedAsync()
	{
		var httpContext = Context.GetHttpContext();

		var brandId = httpContext?.GetBrandId();
		var playerTokenData = httpContext?.Items.GetPlayerTokenData();

		if (playerTokenData is null)
		{
			await Clients.Caller.SendAsync("notify", new
			{
				Type = "InvalidPlayer"
			}).ConfigureAwait(false);

			Context.Abort();

			return;
		}

		_logger.LogDebug(
			"Brand Id: {brandId}, PlayerCode: {playerCode}",
			brandId!.Value,
			playerTokenData!.PlayerCode);

		_logger.LogDebug(
			"UserName: {name}, IsAuthenticated: {isAuthenticated}",
			httpContext!.User.Identity!.Name,
			httpContext!.User.Identity.IsAuthenticated);

		await base.OnConnectedAsync().ConfigureAwait(false);
	}
}
