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
