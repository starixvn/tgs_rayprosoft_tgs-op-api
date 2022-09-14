using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.GameProviders.Pluggable.Client;
using Tgs.ServiceUtility;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Exceptions;

namespace TGS.Services.Operator.ApplicationServices.Game;

public class GetBetConstructHtmlContentQueryService : IGetService<GetBetConstructHtmlContentQuery, BetConstructHtmlContentInfo>
{
    private readonly IGameProvidersOperatorPluggableClient _pluggableClient;

    public GetBetConstructHtmlContentQueryService(
        IGameProvidersOperatorPluggableClient pluggableClient
        )
    {
        _pluggableClient = pluggableClient ?? throw new ArgumentNullException(nameof(pluggableClient));
    }

    public async ValueTask<BetConstructHtmlContentInfo> GetAsync(GetBetConstructHtmlContentQuery query, CancellationToken cancellationToken = default)
    {
        var isMobile = query.PlayerTokenData.PlatformType == PlatformType.Mobile;

        var bcUri = await _pluggableClient
            .GetGameUrlAsync(
                query.PlayerTokenData.PlayerCode,
                query.PlayerTokenData.LoginRecordId,
                isMobile,
                2100001,
                query.CultureName,
                query.IP,
                null,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (bcUri == null)
        {
            throw new GameProviderUnderMaintenanceException();
        }

        var html =
            "<!DOCTYPE html>" +
            "<html lang=\"en\">" +
            "<head>" +
                "<meta charset=\"UTF-8\">" +
                "<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\">" +
                "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">" +
                "<title>BetConstruct</title>" +
                "<style>" +
                    "#bcsportsbookcontainer {" +
                        "float: left;" +
                        "width: 100%;" +
                        "height: 700px;" +
                        "position: relative;" +
                        "overflow: hidden;" +
                    "}" +

                    "#bcsportsbookcontainer iframe {" +
                        "height: 800px;" +
                        "position: absolute;" +
                        "top: ­100px;" +
                        "width: 100%;" +
                        "left: 0;" +
                    "}" +
                "</style>" +
            "</head>" +
            "<body>" +
                "<div id=\"bcsportsbookcontainer\"></div>" +
                $"<script id=\"bcsportsbook\" src=\"{bcUri}\"></script>" +
            "</body>" +
            "</html>";

        return new BetConstructHtmlContentInfo()
        {
            Html = html
        };
    }
}
