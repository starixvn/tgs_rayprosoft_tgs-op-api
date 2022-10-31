using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using TGS.Services.Operator.ApplicationServices.Localization;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LocalizationController : ControllerBase
{
	[HttpGet]
	public ValueTask<IDictionary<string, string>> GetLocalizeDictionaryAsync(
		[FromServices] IGetService<GetAllLocalizationStringsQuery, IDictionary<string, string>> getService)
	{
		var currentCultureName = CultureInfo.CurrentUICulture.Name;

		var query = new GetAllLocalizationStringsQuery(currentCultureName);

		return getService.GetAsync(query);
	}
}
