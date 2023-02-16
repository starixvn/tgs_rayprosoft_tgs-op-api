using Tgs.GameProviders.Pluggable.Client;
using Tgs.ServiceUtility;
using TGS.Core.Models.Exceptions;

namespace TGS.Services.Operator.ApplicationServices.Game;

internal class GetSM01HtmlContentQueryService : IGetService<GetSM01HtmlContentQuery, SM01HtmlContentInfo>
{
	private readonly IGameProvidersOperatorPluggableClient _client;

	public GetSM01HtmlContentQueryService(
		IGameProvidersOperatorPluggableClient client)
	{
		_client = client ?? throw new ArgumentNullException(nameof(client));
	}

	public async ValueTask<SM01HtmlContentInfo> GetAsync(GetSM01HtmlContentQuery query, CancellationToken cancellationToken = default)
	{
		var loginInfo = await _client
			.GetGameSM01PlatformLoginInfoAsync(query.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		if (loginInfo == null)
		{
			throw new GameProviderUnderMaintenanceException();
		}

		var html =
			"<!DOCTYPE html>" +
			"<html>" +
			"<head>" +
				"<meta charset=\"UTF-8\">" +
				"<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\">" +
				"<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">" +
				"<title>SM01</title>" +
				"<script>" +
					"window.onload = function(){" +
						"document.getElementById(\"submit\").click();" +
					"}" +
				"</script>" +
			"</head>" +
			"<body>" +
				"<form action=\"" + loginInfo.ApiUrl + "/auth/platformLogin\" method=\"post\">" +
					"<input type=\"text\" name=\"merchantId\" value=\"" + loginInfo.MerchantId + "\" style=\"display:none\" />" +
					"<input type=\"text\" name=\"customerName\" value=\"" + loginInfo.CustomerName + "\" style=\"display:none\" />" +
					"<input type=\"text\" name=\"timestamp\" value=\"" + loginInfo.Timestamp + "\" style=\"display:none\" />" +
					"<input type=\"text\" name=\"sign\" value=\"" + loginInfo.Sign + "\" style=\"display:none\" />" +
					"<input type=\"submit\" id=\"submit\" style=\"display:none\" />" +
				"</form>" +
			"</body>" +
			"</html>";

		return new SM01HtmlContentInfo
		{
			Html = html
		};
	}
}
