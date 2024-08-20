using Microsoft.AspNetCore.Mvc;
using Tgs.BrandCertificate.Clients;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator.ApplicationServices.Brand;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/brand/social")]
[ApiController]
public class BrandSocialMediaController : ControllerBase
{
	[HttpGet(Name = "ListSocialMediaSettings")]
	[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
	public IAsyncEnumerable<SocialMediaSettingViewModel> ListSocialMediaSettingsAsync(
		[FromServices] IQueryService<ListSocialMediaSettingsQuery, SocialMediaSetting> queryService)
	{
		var brandId = HttpContext.GetBrandId().Value;

		var query = new ListSocialMediaSettingsQuery(brandId);

		return queryService.QueryAsync(query, HttpContext.RequestAborted)
			.Select(setting => new SocialMediaSettingViewModel
			{
				Id = setting.Id,
				Name = setting.Name,
				Type = setting.Type,
				Url = setting.Url
			});
	}
}