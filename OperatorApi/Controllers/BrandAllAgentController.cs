using Microsoft.AspNetCore.Mvc;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using Tgs.ServiceUtility;
using TGS.Services.Operator.ApplicationServices.BrandAllAgent;
using TGS.Apis.OperatorApi.Models.Messages;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BrandAllAgentController : ControllerBase
{
	[HttpGet("check", Name = "GetBrandAllAgentSetting")]
	[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = true)]
	public async ValueTask<BrandAllAgentResponse> GetBrandAllAgentSettingAsync(
		[FromServices] IGetService<GetBrandAllAgentQuery, bool> getService)
	{
		var brandId = HttpContext.Items.GetBrandId();

		var query = new GetBrandAllAgentQuery(brandId);

		var response = await getService.GetAsync(query);

		return new BrandAllAgentResponse
		{
			SwitchOn = response
		};
	}
}
