#nullable enable

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Tgs.Notification.Client;
using TGS.Apis.OperatorApi.Extensions;

namespace TGS.Apis.OperatorApi.SignalR;

public class NotifyHub(INotificationClient notificationClient, ILogger<NotifyHub> logger) : Hub
{
	private readonly ILogger<NotifyHub> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly INotificationClient _notificationClient = notificationClient ?? throw new ArgumentNullException(nameof(notificationClient));

	public async ValueTask AcceptedNotificationAsync(Guid id)
	{
		var httpContext = Context.GetHttpContext();
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

		await _notificationClient.AcceptedNotificationAsync(
			playerTokenData.PlayerCode,
			id,
			Context.ConnectionAborted).ConfigureAwait(false);
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

		await _notificationClient.ResendNotificationsAsync(
			playerTokenData.PlayerCode,
			Context.ConnectionAborted).ConfigureAwait(false);
	}
}
