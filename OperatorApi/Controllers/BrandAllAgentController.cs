using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.BrandAllAgent;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BrandAllAgentController : ControllerBase
{
	[HttpGet("check", Name = "CheckBrandAllAgentSetting")]
	[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
	public async ValueTask<BrandAllAgentSwitchOnResponse> CheckBrandAllAgentSettingAsync(
		[FromServices] IGetService<GetBrandAllAgentSwitchOnQuery, bool> getService)
	{
		var brandId = HttpContext.Items.GetBrandId();

		var query = new GetBrandAllAgentSwitchOnQuery(brandId);

		var switchOn = await getService.GetAsync(query);

		return new BrandAllAgentSwitchOnResponse
		{
			SwitchOn = switchOn
		};
	}

	[HttpGet(Name = "GetBrandAllAgentTurnoverConditions")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public IAsyncEnumerable<BrandAllAgentTurnoverConditionsResponse> GetBrandAllAgentTurnoverConditionsAsync(
		[FromServices] IQueryService<GetBrandAllAgentTurnoverConditionsQuery, BrandAllAgentTurnover> getService)
	{
		var brandId = HttpContext.Items.GetBrandId();

		var query = new GetBrandAllAgentTurnoverConditionsQuery(brandId);

		return getService.QueryAsync(query).Select(x => new BrandAllAgentTurnoverConditionsResponse
		{
			Rank = x.Rank,
			AllAgentGameType = x.AllAgentGameType,
			ActiveNumber = x.ActiveNumber,
			Turnover = x.Turnover,
			RebatePercentage = x.RebatePercentage
		});
	}
}