#nullable enable

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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

		if (httpContext is null)
			_logger.LogDebug("HttpContext is null");

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

		var claims = new List<Claim>
		{
			new("brand_id", brandId!.Value.ToString()),
			new (ClaimTypes.NameIdentifier, playerTokenData!.PlayerCode)
		};

		var authProperties = new AuthenticationProperties
		{
			ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(5D),
			IsPersistent = true
		};

		var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

		await httpContext!.SignInAsync(
			new ClaimsPrincipal(claimsIdentity),
			authProperties).ConfigureAwait(false);

		await base.OnConnectedAsync().ConfigureAwait(false);
	}
}
