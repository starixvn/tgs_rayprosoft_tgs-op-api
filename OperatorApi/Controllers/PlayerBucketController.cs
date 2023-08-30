using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Services.Operator.ApplicationServices.Withdraw;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/player/bucket")]
[ApiController]
public class PlayerBucketController : ControllerBase
{
	[HttpGet("amount", Name = "GetWithdrawRequireAmount")]
	[HttpGet("~/api/withdraw/player/amount")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<decimal> GetWithdrawRequireAmountAsync(
		[FromServices] IGetService<GetRequireBucketAmountQuery, decimal> getService)
	{
		var brandId = HttpContext.GetBrandId()!.Value;
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var query = new GetRequireBucketAmountQuery(brandId, playerTokenData.PlayerCode);
		var wageringRequirement = await getService.GetAsync(query).ConfigureAwait(false);

		return wageringRequirement;
	}
}
