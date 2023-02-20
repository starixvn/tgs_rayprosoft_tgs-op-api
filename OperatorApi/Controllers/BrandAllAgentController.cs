using Microsoft.AspNetCore.Mvc;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using Tgs.ServiceUtility;
using TGS.Services.Operator.ApplicationServices.BrandAllAgent;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BrandAllAgentController : ControllerBase
{
	[HttpGet("check", Name = "GetBrandAllAgentSetting")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public ValueTask<bool> GetBrandAllAgentSettingAsync(
		[FromServices] IGetService<GetBrandAllAgentQuery, bool> getService)
	{
		var brandId = HttpContext.Items.GetBrandId();

		var query = new GetBrandAllAgentQuery(brandId);

		return getService.GetAsync(query);
	}
}
