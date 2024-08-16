using Microsoft.AspNetCore.Mvc;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator.ApplicationServices.Withdraw;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/player/bucket")]
[ApiController]
public class PlayerBucketController : ControllerBase
{
	[HttpGet("type/amount", Name = "GetAllTypeBucketAmounts")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public IAsyncEnumerable<TypeBucketResultViewModel> GetAllTypeBucketAmountsAsync(
		[FromServices] IQueryService<GetRequireBucketAmountQuery, TypeBucketResult> queryService)
	{
		var brandId = HttpContext.GetTgsBrandId()!.Value;
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var query = new GetRequireBucketAmountQuery(brandId, playerTokenData.PlayerCode);

		return queryService.QueryAsync(query, HttpContext.RequestAborted)
			.Select(result => new TypeBucketResultViewModel
			{
				BucketType = result.BucketType,
				Amount = result.Result.Amount
			});
	}

	[HttpGet("amount", Name = "GetWithdrawRequireAmount")]
	[HttpGet("~/api/withdraw/player/amount")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<decimal> GetWithdrawRequireAmountAsync(
		[FromServices] IGetService<GetRequireBucketAmountQuery, decimal> getService)
	{
		var brandId = HttpContext.GetTgsBrandId()!.Value;
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();

		var query = new GetRequireBucketAmountQuery(brandId, playerTokenData.PlayerCode);
		var wageringRequirement = await getService.GetAsync(query).ConfigureAwait(false);

		return wageringRequirement;
	}
}