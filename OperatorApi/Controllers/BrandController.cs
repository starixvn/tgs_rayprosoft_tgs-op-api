using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Brand;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BrandController : ControllerBase
{
	[HttpGet]
	[Description("Get a brand")]
	[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
	public async ValueTask<BrandInfoResponse> GetBrandAsync(
		[FromServices] IGetService<BrandQuery, BrandInfo> queryService,
		[FromHeader(Name = "Agent-Invitation-Code")] string invitationCode)
	{
		var brandId = HttpContext.GetBrandId().Value;

		var brandInfo = await queryService
			.GetAsync(new BrandQuery(brandId))
			.ConfigureAwait(false);

		return new BrandInfoResponse
		{
			BrandId = brandId,
			Code = brandInfo.Code,
			DesktopLogoUrl = brandInfo.DesktopLogoUrl,
			HasInvitationCode = !string.IsNullOrWhiteSpace(invitationCode),
			LoadingUrl = brandInfo.LoadingUrl,
			MobileLogoUrl = brandInfo.MobileLogoUrl,
			SmsEnable = brandInfo.SmsEnabled,
			PlayerIdentityAuthenticationEnabled = brandInfo.PlayerIdentityEnabled,
			PlayerRegisterFlowSettings = brandInfo.PlayerRegisterFlowSettings
		};
	}
}
