using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator.ApplicationServices.Brand;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/brand/player-contract")]
[ApiController]
public class BrandPlayerContractController : ControllerBase
{
	[HttpGet(Name = "ListBrandPlayerContractDisplayType")]
	public async IAsyncEnumerable<BrandPlayerContractDisplayViewModel> ListBrandPlayerContractDisplayTypeAsync(
		[FromServices] IQueryService<BrandPlayerContractDisplayQuery, BrandPlayerContractDisplayInfo> queryService)
	{
		var brandId = HttpContext.Items.GetBrandId();

		var info = await queryService
			.QueryAsync(new BrandPlayerContractDisplayQuery(brandId), HttpContext.RequestAborted)
			.ToArrayAsync(HttpContext.RequestAborted);

		foreach (var item in info)
		{
			yield return new BrandPlayerContractDisplayViewModel
			{
				Name = item.Name.ToString(),
				IsVisualized = item.IsVisualized,
			};
		}
	}
}