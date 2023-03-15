using System.Data.SqlClient;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Models.Messages;
using TGS.Services.Operator.ApplicationServices.Vip;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
public class VipController : ControllerBase
{
	[HttpGet(Name = "GetVipLevelList")]
	[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
	public async Task<PagingResponse<VipLevelModel>> GetVipLevelListAsync(
		[FromQuery] PagingRequest pagingRequest,
		[FromServices] IQueryService<GetVipLevelListQuery, VipRecord> getVipLevelsService,
		[FromServices] IGetService<GetVipLevelListQuery, int> countVipLevelsService)
	{
		var brandId = HttpContext.Items.GetBrandId();

		var paging = new PageableRequest();
		if (pagingRequest.ShowAll)
			paging.ShowAll = true;
		else
		{
			paging.Page = pagingRequest.Page;
			paging.Rows = pagingRequest.Rows;
			paging.OrderBy = pagingRequest.IsDesc ? SortOrder.Descending : SortOrder.Ascending;
			paging.SortBy = pagingRequest.SortBy;
		}

		var query = new GetVipLevelListQuery(brandId, paging);

		return new PagingResponse<VipLevelModel>()
		{
			TotalCount = await countVipLevelsService
				.GetAsync(query)
				.ConfigureAwait(false),
			Items = getVipLevelsService
				.QueryAsync(query)
				.Select(vip => new VipLevelModel
				{
					Level = vip.Level,
					LevelUpAccumulateBetAmount = vip.LevelUpAccumulateBetAmount,
					LevelUpAccumulateDeposit = vip.LevelUpAccumulateDeposit,
					LevelKeepAccumulateBetAmount = vip.LevelKeepAccumulateBetAmount,
					LevelKeepAccumulateDeposit = vip.LevelKeepAccumulateDeposit,
					BirthBenefit = vip.BirthBenefit,
					PromotionBenefit = vip.PromotionBenefit,
					QuarterlyBenefit = vip.QuarterlyBenefit,
					LiveDealerRebate = vip.LiveDealerRebate,
					SlotRebate = vip.SlotRebate,
					CardGameRebate = vip.CardGameRebate,
					SportsRebate = vip.SportsRebate,
					VirtualSportsRebate = vip.VirtualSportsRebate,
					LotteryRebate = vip.LotteryRebate
				})
				.ToEnumerable()
		};
	}
}
