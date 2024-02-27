using System.ComponentModel;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.BrandLiveChat;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BrandLiveChatController : ControllerBase
{
	[HttpGet]
	[Description("Get a brand's live chat")]
	[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
	public async Task<BrandLiveChatModel> GetBrandLiveChatAsync(
		[FromServices] IGetService<BrandLiveChatQuery, BrandLiveChatInfo> queryService)
	{
		var brandId = HttpContext.Items.GetBrandId();

		var query = new BrandLiveChatQuery(brandId);

		var info = await queryService.GetAsync(query).ConfigureAwait(false);

		return info == null
			? null
			: new BrandLiveChatModel()
			{
				Name = info.Name,
				Setting = string.IsNullOrEmpty(info.Setting)
					? JsonDocument.Parse("{}")
					: JsonDocument.Parse(info.Setting)
			};
	}
}