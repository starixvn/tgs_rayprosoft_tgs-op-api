using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.BrandLanguage;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BrandLanguageController : ControllerBase
{
	[HttpGet]
	[Description("Get a brand's languages")]
	[AuthorizePlayerToken(AllowNullOrEmpty = true, NeedAuthorize = false)]
	public IAsyncEnumerable<BrandLanguageModel> GetBrandLanguagesAsync(
	   [FromServices] IQueryService<BrandLanguageQuery, BrandLanguageInfo> queryService)
	{
		var brandId = HttpContext.Items.GetBrandId();

		var query = new BrandLanguageQuery(brandId);
		var info = queryService.QueryAsync(query);

		return info.Select(x =>
			new BrandLanguageModel()
			{
				CultureCode = x.CultureCode
			});
	}
}