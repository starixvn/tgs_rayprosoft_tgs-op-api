using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Infrastructures.Json;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Banner;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BannerController : ControllerBase
{
    [HttpGet]
    [AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
    public IAsyncEnumerable<BannerResponse> GetBannersAsync(
        [FromServices] IQueryService<GetBannersQuery, BannerInfo> queryService,
        [FromServices] IOptions<JsonOptions> optionsAccessor,
        [FromServices] IJsonSerializationProvider jsonSerializationProvider)
    {
        var jsonOptions = optionsAccessor.Value;

        var brandId = HttpContext.Items.GetBrandId();
        var cultureCode = CultureInfo.CurrentUICulture.Name;

        var query = new GetBannersQuery(brandId);

        return from info in queryService.QueryAsync(query)
               let loc = string.IsNullOrWhiteSpace(info.Localization)
                ? null
                : jsonSerializationProvider.DeserializeFromString<IDictionary<string, BannerContent>>(
                    info.Localization,
                    jsonOptions.JsonSerializerOptions)
               let content = loc?.ContainsKey(cultureCode) == true
                ? loc[cultureCode]
                : loc?.ContainsKey("en-US") == true
                ? loc["en-US"]
                : null
               select new BannerResponse
               {
                   Id = info.Id,
                   Title = content?.Title ?? info.Title,
                   DesktopBannerUrl = content?.DesktopBannerUrl,
                   MobileBannerUrl = content?.MobileBannerUrl,
                   NavigateUrl = content?.NavigateUrl,
                   PictureAlt = content?.PictureAlt
               };
    }
}
